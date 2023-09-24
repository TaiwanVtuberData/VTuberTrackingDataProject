using Common.Types;
using Common.Types.Basic;
using Common.Utils;
using FetchTwitchRecord.Extensions;
using log4net;
using System.Collections.Immutable;
using System.Xml;
using TwitchLib.Api;

namespace FetchTwitchStatistics;
public class Fetcher {
    private static readonly ILog log = LogManager.GetLogger(typeof(Fetcher));

    public record Credential(string ClientId, string Secret);

    private readonly Credential _Credential;
    private readonly DateTimeOffset CurrentTime;

    public Fetcher(Credential credential, DateTimeOffset currentTime) {
        _Credential = credential;
        CurrentTime = currentTime;
    }

    public bool GetAll(string userId, out TwitchStatistics statistics, out TopVideosList topVideoList, out LiveVideosList liveVideosList) {
        TwitchAPI api = CreateTwitchApiInstance();

        ulong? nullableFollowerCount = api.GetChannelFollwerCount(userId, log);

        var (successRecentViewCount, medianViewCount, popularity, highestViewCount, highestViewdVideoID, topVideoList_) = GetChannelRecentViewStatistic(userId, api);

        LiveVideosList liveVideos = GetLiveVideosList(userId, api);

        if (nullableFollowerCount != null && successRecentViewCount) {
            statistics = new TwitchStatistics {
                RecentMedianViewCount = medianViewCount,
                RecentPopularity = popularity,
                RecentHighestViewCount = highestViewCount,
                HighestViewedVideoURL = (highestViewdVideoID != "") ? $"https://www.twitch.tv/videos/{highestViewdVideoID}" : "",
            };
            statistics.UpdateFollowerCount(nullableFollowerCount.Value);

            topVideoList = topVideoList_;
            liveVideosList = liveVideos;
            return true;
        } else {
            statistics = new TwitchStatistics();
            topVideoList = new TopVideosList();
            liveVideosList = new LiveVideosList();
            return false;
        }
    }

    private (bool Success, ulong MedianViewCount, ulong Popularity, ulong HighestViewCount, string HighestViewedVideoID, TopVideosList TopVideosList_)
        GetChannelRecentViewStatistic(string userId, TwitchAPI api) {
        List<Tuple<DateTimeOffset, string, ulong>> viewCountList = new();

        string afterCursor = "";
        TopVideosList topVideosList = new();

        while (afterCursor != null) {
            TwitchLib.Api.Helix.Models.Videos.GetVideos.GetVideosResponse? videoResponseResult = null;
            bool hasResponse = false;
            for (int i = 0; i < 2; i++) {
                try {
                    var videosResponse =
                        api.Helix.Videos.GetVideosAsync(
                            userId: userId,
                            after: afterCursor,
                            first: 100,
                            period: TwitchLib.Api.Core.Enums.Period.Month, // this parameter doesn't work at all
                            sort: TwitchLib.Api.Core.Enums.VideoSort.Time,
                            type: TwitchLib.Api.Core.Enums.VideoType.Archive // Archive type probably is past broadcasts
                            );
                    videoResponseResult = videosResponse.Result;

                    hasResponse = true;
                    break;
                } catch {
                }
            }

            if (!hasResponse || videoResponseResult is null) {
                return (false, 0, 0, 0, "", new());
            }

            afterCursor = videoResponseResult.Pagination.Cursor;

            foreach (TwitchLib.Api.Helix.Models.Videos.GetVideos.Video video in videoResponseResult.Videos) {
                string videoId = video.Id;
                ulong viewCount = (ulong)video.ViewCount;
                DateTimeOffset publishTime = DateTimeOffset.Parse(video.PublishedAt);

                TimeSpan publishPastTime = CurrentTime - publishTime;
                if (TimeSpan.Zero < publishPastTime && publishPastTime < TimeSpan.FromDays(30)) {
                    // there is currently no way to know which video is streaming
                    // 0 view count is observed to be a livestream
                    // FIXME: the value might not be 0 when livestreaming
                    if (viewCount != 0) {
                        viewCountList.Add(new(publishTime, videoId, viewCount));
                    }

                    try {
                        topVideosList.Insert(new VideoInformation {
                            Id = new VTuberId(userId),
                            Url = $"https://www.twitch.tv/videos/{video.Id}",
                            Title = video.Title,
                            ThumbnailUrl = video.ThumbnailUrl,
                            PublishDateTime = XmlConvert.ToDateTime(video.PublishedAt, XmlDateTimeSerializationMode.Utc),
                            ViewCount = viewCount,
                        });
                    } catch {
                    }
                }
            }
        }

        if (viewCountList.Count <= 0) {
            // return true because it is a successful result
            return (true, 0, 0, 0, "", new());
        }

        ImmutableList<Tuple<DateTimeOffset, string, ulong>> immutableList = viewCountList.ToImmutableList();

        ulong medianViews = NumericUtility.GetMedian(immutableList);
        Tuple<DateTimeOffset, string, ulong> largest = NumericUtility.GetLargest(immutableList);
        decimal popularity = NumericUtility.GetPopularity(immutableList, CurrentTime.UtcDateTime);
        return (true, medianViews, (ulong)popularity, largest.Item3, largest.Item2, topVideosList);
    }

    private LiveVideosList GetLiveVideosList(string userId, TwitchAPI api) {
        LiveVideosList rLst = GetScheduleLiveVideosList(userId, api);

        LiveVideoInformation? livestream = GetActiveStream(userId, api);

        if (livestream != null) {
            rLst.Add(livestream);
        }

        return rLst;
    }

    private LiveVideoInformation? GetActiveStream(string userId, TwitchAPI api) {
        TwitchLib.Api.Helix.Models.Streams.GetStreams.GetStreamsResponse? streamResponseResult = null;
        bool hasResponse = false;
        for (int i = 0; i < 2; i++) {
            try {

                var streamResponse = api.Helix.Streams.GetStreamsAsync(userIds: new List<string>() { userId });
                streamResponseResult = streamResponse.Result;

                hasResponse = true;
                break;
            } catch {
            }
        }

        if (!hasResponse || streamResponseResult is null) {
            return null;
        }

        TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream[]? streams = streamResponseResult.Streams;

        if (streams is null || streams.Length != 1) {
            return null;
        }

        TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream stream = streams[0];

        return (new LiveVideoInformation {
            Id = new VTuberId(userId),
            Url = $"https://www.twitch.tv/{stream.UserLogin}",
            Title = stream.Title,
            ThumbnailUrl = stream.ThumbnailUrl,
            PublishDateTime = stream.StartedAt.ToUniversalTime(),
            VideoType = LiveVideoType.live,
        });
    }

    private static LiveVideosList GetScheduleLiveVideosList(string userId, TwitchAPI api) {
        LiveVideosList rLst = new();

        TwitchLib.Api.Helix.Models.Schedule.GetChannelStreamSchedule.GetChannelStreamScheduleResponse? scheduleResponseResult = null;
        bool hasResponse = false;
        for (int i = 0; i < 2; i++) {
            try {

                var scheduleResponse = api.Helix.Schedule.GetChannelStreamScheduleAsync(
                    broadcasterId: userId,
                    first: 10
                    );
                scheduleResponseResult = scheduleResponse.Result;

                hasResponse = true;
                break;
            } catch {
            }
        }

        if (!hasResponse || scheduleResponseResult is null) {
            return new();
        }

        TwitchLib.Api.Helix.Models.Schedule.ChannelStreamSchedule? schedule = scheduleResponseResult.Schedule;

        if (schedule.Segments is null) {
            return new();
        }

        foreach (var segment in schedule.Segments) {
            try {
                rLst.Add(new LiveVideoInformation {
                    Id = new VTuberId(userId),
                    Url = $"https://www.twitch.tv/{schedule.BroadcasterLogin}",
                    Title = segment.Title,
                    ThumbnailUrl = "",
                    PublishDateTime = segment.StartTime.ToUniversalTime(),
                    VideoType = LiveVideoType.upcoming,
                });
            } catch {
            }
        }

        return rLst;
    }

    private TwitchAPI CreateTwitchApiInstance() {
        TwitchAPI api = new();
        api.Settings.ClientId = _Credential.ClientId;
        api.Settings.Secret = _Credential.Secret;

        return api;
    }
}