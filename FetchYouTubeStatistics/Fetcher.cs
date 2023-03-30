using Common.Types;
using Common.Utils;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;

namespace FetchYouTubeStatistics;
public class Fetcher {
    public string ApiKey { get; set; }
    private readonly YouTubeService youtubeService;
    private readonly DateTime CurrentTime;

    public Fetcher(string ApiKey, DateTime currentTime) {
        this.ApiKey = ApiKey;
        youtubeService = new YouTubeService(new BaseClientService.Initializer() { ApiKey = this.ApiKey });

        CurrentTime = currentTime;
    }

    public (Dictionary<string, YouTubeStatistics>, TopVideosList, LiveVideosList) GetAll(List<string> lstChannelId) {
        List<Google.Apis.YouTube.v3.Data.Channel> lstChannelInfo = GetChannelInfoList(lstChannelId);

        Dictionary<string, YouTubeStatistics> rDict = GetChannelStatistics(lstChannelInfo);
        TopVideosList rVideoList = new(videoCount: 1000);
        LiveVideosList rLiveVideoList = new();

        GetChannelRecentViewStatistic(lstChannelInfo, ref rDict, ref rVideoList, ref rLiveVideoList);

        return (rDict, rVideoList, rLiveVideoList);
    }

    private List<Google.Apis.YouTube.v3.Data.Channel> GetChannelInfoList(List<string> lstChannelId) {
        List<Google.Apis.YouTube.v3.Data.Channel> rLst = new(lstChannelId.Count);

        // When querying multipie channel statistics, only 50 videos at a time is allowed
        List<string> lstIdRequest = Generate50IdsStringList(lstChannelId);

        foreach (string idRequest in lstIdRequest) {
            IList<Google.Apis.YouTube.v3.Data.Channel>? lstChannelInfo = GetChannelStatisticsResponse(idRequest);

            if (lstChannelInfo is not null)
                rLst.AddRange(lstChannelInfo);
        }

        return rLst;
    }

    private IList<Google.Apis.YouTube.v3.Data.Channel>? GetChannelStatisticsResponse(string idRequestString) {
        ChannelsResource.ListRequest channelsListRequest = youtubeService.Channels.List("statistics, contentDetails");
        channelsListRequest.Id = idRequestString;
        channelsListRequest.MaxResults = 50;

        IList<Google.Apis.YouTube.v3.Data.Channel>? responseItems = null;
        bool hasResponse = false;
        // try for three times
        for (int i = 0; i < 3; i++) {
            Google.Apis.YouTube.v3.Data.ChannelListResponse channelsListResponse = channelsListRequest.Execute();
            responseItems = channelsListResponse.Items;

            if (responseItems is null) {
                continue;
            }

            hasResponse = true;
            break;
        }

        if (!hasResponse) {
            return null;
        }

        return responseItems;
    }

    private static Dictionary<string, YouTubeStatistics> GetChannelStatistics(List<Google.Apis.YouTube.v3.Data.Channel> lstChannelInfo) {
        Dictionary<string, YouTubeStatistics> rDict = new(lstChannelInfo.Count);

        foreach (Google.Apis.YouTube.v3.Data.Channel channelInfo in lstChannelInfo) {
            // do not add channel if already in rDict
            if (rDict.ContainsKey(channelInfo.Id)) {
                continue;
            }

            ulong? viewCount = channelInfo.Statistics.ViewCount;
            ulong? subscriberCount = channelInfo.Statistics.SubscriberCount;

            YouTubeStatistics statistics = new() {
                ViewCount = viewCount.GetValueOrDefault(0),
            };
            statistics.UpdateSubscriberCount(subscriberCount.GetValueOrDefault(0));

            rDict.Add(channelInfo.Id, statistics);
        }

        return rDict;
    }

    private Dictionary<string, DateTime> GetChannelRecentVideoList(Google.Apis.YouTube.v3.Data.Channel channelInfo) {
        Dictionary<string, DateTime> dictIdTime = new();

        string uploadsListId = channelInfo.ContentDetails.RelatedPlaylists.Uploads;
        string nextPageToken = "";
        while (nextPageToken != null) {
            PlaylistItemsResource.ListRequest playlistItemsListRequest = youtubeService.PlaylistItems.List("snippet,status");
            playlistItemsListRequest.PlaylistId = uploadsListId;
            playlistItemsListRequest.MaxResults = 1000;
            playlistItemsListRequest.PageToken = nextPageToken;

            // Retrieve the list of videos uploaded to the authenticated user's channel.
            Google.Apis.YouTube.v3.Data.PlaylistItemListResponse? playlistItemsListResponse;
            try {
                playlistItemsListResponse = playlistItemsListRequest.Execute();
            } catch {
                // return true because it is a successful result
                return new();
            }

            foreach (Google.Apis.YouTube.v3.Data.PlaylistItem playlistItem in playlistItemsListResponse.Items) {
                DateTime? videoPublishTime = playlistItem.Snippet.PublishedAt;
                string videoPrivacyStatus = playlistItem.Status.PrivacyStatus;
                string videoId = playlistItem.Snippet.ResourceId.VideoId;

                // only add video id if its publish time is within 30 days
                if (videoPublishTime is not null) {
                    DateTime publishTime = videoPublishTime.Value;
                    if ((DateTime.UtcNow - publishTime) < TimeSpan.FromDays(30)) {
                        dictIdTime.Add(videoId, videoPublishTime.Value);
                    }
                }
            }

            nextPageToken = playlistItemsListResponse.NextPageToken;
        }

        return dictIdTime;
    }

    private void GetChannelRecentViewStatistic(
        List<Google.Apis.YouTube.v3.Data.Channel> lstChannelInfo,
        ref Dictionary<string, YouTubeStatistics> dictIdStatistics,
        ref TopVideosList topVideosList,
        ref LiveVideosList liveVideosList) {
        foreach (Google.Apis.YouTube.v3.Data.Channel channelInfo in lstChannelInfo) {
            Dictionary<string, DateTime> dictIdTime = GetChannelRecentVideoList(channelInfo);

            // When querying multipie video statistics, only 50 videos at a time is allowed
            List<string> idRequestList = Generate50IdsStringList(dictIdTime.Keys.ToList());

            // The Tuple is video ID and video view count
            List<Tuple<DateTime, string, ulong>> lstIdViewCount = new();
            foreach (string idRequestString in idRequestList) {
                VideosResource.ListRequest videosListRequest = youtubeService.Videos.List("id,snippet,statistics,liveStreamingDetails");
                videosListRequest.Id = idRequestString;

                Google.Apis.YouTube.v3.Data.VideoListResponse videoListResponse = videosListRequest.Execute();

                foreach (Google.Apis.YouTube.v3.Data.Video video in videoListResponse.Items) {
                    ulong? viewCount = video.Statistics.ViewCount;
                    DateTime? publishTime = video.Snippet.PublishedAt;
                    // if there is view count and the video is not (streaming or upcoming livestream)
                    if (viewCount is not null
                        && publishTime is not null
                        && !LiveVideoTypeConvert.IsLiveVideoType(video.Snippet.LiveBroadcastContent)) {
                        lstIdViewCount.Add(new(publishTime.GetValueOrDefault(DateTime.UnixEpoch).ToUniversalTime(), video.Id, viewCount.Value));
                    }

                    topVideosList.Insert(new VideoInformation {
                        Id = channelInfo.Id,
                        Url = $"https://www.youtube.com/watch?v={video.Id}",
                        Title = video.Snippet.Title,
                        ThumbnailUrl = video.Snippet.Thumbnails.Medium.Url,
                        PublishDateTime = video.Snippet.PublishedAt.GetValueOrDefault(DateTime.UnixEpoch).ToUniversalTime(),
                        ViewCount = viewCount.GetValueOrDefault(),
                    });

                    if (LiveVideoTypeConvert.IsLiveVideoType(video.Snippet.LiveBroadcastContent)) {
                        DateTime startTime =
                            (video.LiveStreamingDetails.ActualStartTime ?? video.LiveStreamingDetails.ScheduledStartTime.GetValueOrDefault(DateTime.UnixEpoch))
                            .ToUniversalTime();

                        liveVideosList.Add(new LiveVideoInformation {
                            Id = channelInfo.Id,
                            Url = $"https://www.youtube.com/watch?v={video.Id}",
                            Title = video.Snippet.Title,
                            ThumbnailUrl = video.Snippet.Thumbnails.Medium.Url,
                            PublishDateTime = startTime,
                            VideoType = LiveVideoTypeConvert.FromString(video.Snippet.LiveBroadcastContent),
                        });
                    }
                }
            }

            ulong medianViews = 0;
            ulong popularity = 0;
            ulong highestViews = 0;
            string highestViewedUrl = "";
            if (lstIdViewCount.Count != 0) {
                medianViews = NumericUtility.GetMedian(lstIdViewCount);
                popularity = (ulong)NumericUtility.GetPopularity(lstIdViewCount, CurrentTime);
                Tuple<DateTime, string, ulong> largest = NumericUtility.GetLargest(lstIdViewCount);
                highestViews = largest.Item3;
                highestViewedUrl = largest.Item2;


                highestViewedUrl = $"https://www.youtube.com/watch?v={largest.Item2}";
            }

            dictIdStatistics[channelInfo.Id].RecentMedianViewCount = medianViews;
            dictIdStatistics[channelInfo.Id].RecentPopularity = popularity;
            dictIdStatistics[channelInfo.Id].RecentHighestViewCount = highestViews;
            dictIdStatistics[channelInfo.Id].HighestViewedVideoURL = highestViewedUrl;
        }
    }

    private static List<string> Generate50IdsStringList(List<string> KeyList) {
        List<string> ans = new();

        int index;
        // pack 50 ids into a string
        for (index = 0; index < (KeyList.Count) / 50 * 50; index += 50) {
            string idRequestString = "";
            for (int offset = 0; offset < 50; offset++)
                idRequestString += KeyList[index + offset] + ',';
            idRequestString = idRequestString.Substring(0, idRequestString.Length - 1);
            ans.Add(idRequestString);
        }

        // residual
        if (KeyList.Count % 50 != 0) {
            string idRequestStringRes = "";
            for (; index < KeyList.Count; index++) {
                idRequestStringRes += KeyList[index] + ',';
            }
            idRequestStringRes = idRequestStringRes.Substring(0, idRequestStringRes.Length - 1);
            ans.Add(idRequestStringRes);
        }

        return ans;
    }
}