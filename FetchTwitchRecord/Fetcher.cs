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

    public void GetAll(HashSet<string> userIdList, out Dictionary<string, TwitchStatistics> rStatisticDict, out TopVideosList rTopVideosList, out LiveVideosList rLiveVideosList) {
        rStatisticDict = new Dictionary<string, TwitchStatistics>(userIdList.Count);
        rTopVideosList = new();

        // rStatisticDict and rTopVideosList
        foreach (string userId in userIdList) {
            log.Info($"Start getting Twitch info of user ID: {userId}");
            ulong? nullableFollowerCount = twitchAPI.GetChannelFollwerCount(userId, log);

            var (successRecentViewCount, medianViewCount, popularity, highestViewCount, highestViewdVideoID, topVideosList)
                = GetChannelRecentViewStatistic(userId);

            if (nullableFollowerCount != null && successRecentViewCount) {
                TwitchStatistics statistics = new() {
                    RecentMedianViewCount = medianViewCount,
                    RecentPopularity = popularity,
                    RecentHighestViewCount = highestViewCount,
                    HighestViewedVideoURL = (highestViewdVideoID != "") ? $"https://www.twitch.tv/videos/{highestViewdVideoID}" : "",
                };
                statistics.UpdateFollowerCount(nullableFollowerCount.Value);

                rStatisticDict.Add(userId, statistics);

                rTopVideosList.Insert(topVideosList.GetSortedList());
            }
            log.Info($"End   getting Twitch info of user ID: {userId}");
        }

        // rLiveVideosList
        log.Info($"Start getting Twitch live videos");
        rLiveVideosList = GetLiveVideosList(userIdList.ToList());
        log.Info($"End   getting Twitch live videos");
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

    private LiveVideosList GetLiveVideosList(List<string> userIdList) {
        // LiveVideosList rLst = GetScheduleLiveVideosList(userId);

        LiveVideosList rLst = new();

        List<LiveVideoInformation> livestreamList = GetActiveStreams(userIdList);
        rLst.AddRange(livestreamList);

        return rLst;
    }

    private List<LiveVideoInformation> GetActiveStreams(List<string> userIdList) {
        List<LiveVideoInformation> rLst = new(userIdList.Count);

        List<List<string>> chunkedUserIdList = Generate100IdsStringListList(userIdList);
        foreach (List<string> smallUserIdList in chunkedUserIdList) {
            log.Info($"Start getting Twitch active livestreams user IDs: {string.Join(',', smallUserIdList)}");
            GetStreamsResponse? getStreamsResponse = twitchAPI.GetChannelsActiveLivestreams(
                userIdList: smallUserIdList,
                log: log
                );

            if (getStreamsResponse == null) {
                continue;
            }

            TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream[]? streams = getStreamsResponse.Streams;

            if (streams is null) {
                continue;
            }

            foreach (TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream stream in streams) {
                rLst.Add(new LiveVideoInformation {
                    Id = new VTuberId(stream.UserId),
                    Url = $"https://www.twitch.tv/{stream.UserLogin}",
                    Title = stream.Title,
                    ThumbnailUrl = stream.ThumbnailUrl,
                    PublishDateTime = stream.StartedAt.ToUniversalTime(),
                    VideoType = LiveVideoType.live,
                }
                );
            }
            log.Info($"End   getting Twitch active livestreams user IDs: {string.Join(',', smallUserIdList)}");
        }

        return rLst;
    }

    private static List<List<string>> Generate100IdsStringListList(List<string> keyList) {
        return keyList.Chunk(100).Map(e => e.ToList()).ToList();
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