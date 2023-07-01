using Common.Types;
using Common.Utils;
using System.Xml;
using TwitchLib.Api;

namespace FetchTwitchStatistics;
public class Fetcher {
    public record Credential(string clientId, string secret);

    private readonly TwitchAPI api;
    private readonly DateTime CurrentTime;

    public Fetcher(Credential credential, DateTime currentTime) {
        api = new TwitchAPI();
        api.Settings.ClientId = credential.clientId;
        api.Settings.Secret = credential.secret;

        CurrentTime = currentTime;
    }

    public bool GetAll(string userId, out TwitchStatistics statistics, out TopVideosList topVideoList, out LiveVideosList liveVideosList) {
        var (successStatistics, followerCount) = GetChannelStatistics(userId);
        var (successRecentViewCount, medianViewCount, popularity, highestViewCount, highestViewdVideoID, topVideoList_) = GetChannelRecentViewStatistic(userId);
        LiveVideosList liveVideos = GetLiveVideosList(userId);

        if (successStatistics && successRecentViewCount) {
            statistics = new TwitchStatistics {
                RecentMedianViewCount = medianViewCount,
                RecentPopularity = popularity,
                RecentHighestViewCount = highestViewCount,
                HighestViewedVideoURL = (highestViewdVideoID != "") ? $"https://www.twitch.tv/videos/{highestViewdVideoID}" : "",
            };
            statistics.UpdateFollowerCount(followerCount);

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

    private (bool Success, ulong FollowerCount) GetChannelStatistics(string userId) {
        TwitchLib.Api.Helix.Models.Users.GetUserFollows.GetUsersFollowsResponse? usersFollowsResponseResult = null;

        bool hasResponse = false;
        for (int i = 0; i < 2; i++) {
            try {
                var usersFollowsResponse =
                    api.Helix.Users.GetUsersFollowsAsync(
                        first: 100,
                        toId: userId
                        );
                usersFollowsResponseResult = usersFollowsResponse.Result;

                hasResponse = true;
                break;
            } catch {
            }
        }

        if (!hasResponse || usersFollowsResponseResult is null) {
            return (false, 0);
        }

        return (true, (ulong)usersFollowsResponseResult.TotalFollows);
    }

    private (bool Success, ulong MedianViewCount, ulong Popularity, ulong HighestViewCount, string HighestViewedVideoID, TopVideosList TopVideosList_)
        GetChannelRecentViewStatistic(string userId) {
        List<Tuple<DateTime, string, ulong>> viewCountList = new();

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
                DateTime publishTime = DateTime.Parse(video.PublishedAt);

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
                            Id = userId,
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

        ulong medianViews = NumericUtility.GetMedian(viewCountList);
        Tuple<DateTime, string, ulong> largest = NumericUtility.GetLargest(viewCountList);
        decimal popularity = NumericUtility.GetPopularity(viewCountList, CurrentTime);
        return (true, medianViews, (ulong)popularity, largest.Item3, largest.Item2, topVideosList);
    }

    private LiveVideosList GetLiveVideosList(string userId) {
        LiveVideosList rLst = GetScheduleLiveVideosList(userId);

        LiveVideoInformation? livestream = GetActiveStream(userId);

        if (livestream != null) {
            rLst.Add(livestream);
        }

        return rLst;
    }

    private LiveVideoInformation? GetActiveStream(string userId) {
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
            Id = userId,
            Url = $"https://www.twitch.tv/{stream.UserLogin}",
            Title = stream.Title,
            ThumbnailUrl = stream.ThumbnailUrl,
            PublishDateTime = stream.StartedAt.ToUniversalTime(),
            VideoType = LiveVideoType.live,
        });
    }

    private LiveVideosList GetScheduleLiveVideosList(string userId) {
        TwitchLib.Api.Helix.Models.Schedule.GetChannelStreamSchedule.GetChannelStreamScheduleResponse? scheduleResponseResult = null;

        LiveVideosList rLst = new();

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
                    Id = userId,
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