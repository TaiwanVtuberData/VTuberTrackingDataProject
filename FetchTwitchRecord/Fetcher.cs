using Common.Types;
using Common.Types.Basic;
using Common.Utils;
using FetchTwitchRecord.Extensions;
using log4net;
using System.Collections.Immutable;
using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Schedule.GetChannelStreamSchedule;
using TwitchLib.Api.Helix.Models.Streams.GetStreams;
using TwitchLib.Api.Helix.Models.Videos.GetVideos;

namespace FetchTwitchStatistics;
public class Fetcher {
    private static readonly ILog log = LogManager.GetLogger(typeof(Fetcher));

    public record Credential(string ClientId, string Secret);

    private readonly TwitchAPI twitchAPI;
    private readonly DateTimeOffset CurrentTime;

    public Fetcher(Credential credential, DateTimeOffset currentTime) {
        twitchAPI = new();
        twitchAPI.Settings.ClientId = credential.ClientId;
        twitchAPI.Settings.Secret = credential.Secret;

        CurrentTime = currentTime;
    }

    public bool GetAll(string userId, out TwitchStatistics statistics, out TopVideosList topVideoList, out LiveVideosList liveVideosList) {
        ulong? nullableFollowerCount = twitchAPI.GetChannelFollwerCount(userId, log);

        var (successRecentViewCount, medianViewCount, popularity, highestViewCount, highestViewdVideoID, topVideoList_)
            = GetChannelRecentViewStatistic(userId);

        LiveVideosList liveVideos = GetLiveVideosList(userId);

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
        GetChannelRecentViewStatistic(string userId) {
        List<Tuple<DateTimeOffset, string, ulong>> viewCountList = new();

        string afterCursor = "";
        TopVideosList topVideosList = new();

        while (afterCursor != null) {
            GetVideosResponse? getVideosResponse = twitchAPI.GetChannelPastLivestreams(
                userId: userId,
                afterCursor: afterCursor,
                log: log
                );

            if (getVideosResponse == null) {
                return (false, 0, 0, 0, "", new());
            }

            afterCursor = getVideosResponse.Pagination.Cursor;

            foreach (Video video in getVideosResponse.Videos) {
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
                            PublishDateTime = publishTime,
                            ViewCount = viewCount,
                        });
                    } catch (Exception e) {
                        log.Error($"Error calling topVideosList.Insert() when ID: {userId}, Video ID: {video.Id}");
                        log.Error(e.Message, e);
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

    private LiveVideosList GetLiveVideosList(string userId) {
        // LiveVideosList rLst = GetScheduleLiveVideosList(userId);

        LiveVideosList rLst = new();

        LiveVideoInformation? livestream = GetActiveStream(userId);

        if (livestream != null) {
            rLst.Add(livestream);
        }

        return rLst;
    }

    private LiveVideoInformation? GetActiveStream(string userId) {
        GetStreamsResponse? getStreamsResponse = twitchAPI.GetChannelActiveLivestreams(
            userId: userId,
            log: log
            );

        if (getStreamsResponse == null) {
            return null;
        }

        TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream[]? streams = getStreamsResponse.Streams;

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

    private LiveVideosList GetScheduleLiveVideosList(string broadcasterId) {
        LiveVideosList rLst = new();

        GetChannelStreamScheduleResponse? getStreamsResponse = twitchAPI.GetChannelScheduledLivestreams(
            broadcasterId: broadcasterId,
            log: log
            );

        if (getStreamsResponse == null) {
            return new();
        }

        TwitchLib.Api.Helix.Models.Schedule.ChannelStreamSchedule? schedule = getStreamsResponse.Schedule;

        if (schedule.Segments is null) {
            return new();
        }

        foreach (var segment in schedule.Segments) {
            try {
                rLst.Add(new LiveVideoInformation {
                    Id = new VTuberId(broadcasterId),
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
}