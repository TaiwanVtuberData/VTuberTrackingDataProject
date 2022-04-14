using Common.Types;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;

namespace FetchYouTubeStatistics;
public class Fetcher
{
    public string ApiKey { get; set; }
    private readonly YouTubeService youtubeService;

    public Fetcher(string ApiKey)
    {
        this.ApiKey = ApiKey;
        youtubeService = new YouTubeService(new BaseClientService.Initializer() { ApiKey = this.ApiKey });
    }

    public (Dictionary<string, YouTubeStatistics>, TopVideosList) GetAll(List<string> lstChannelId)
    {
        List<Google.Apis.YouTube.v3.Data.Channel> lstChannelInfo = GetChannelInfoList(lstChannelId);

        Dictionary<string, YouTubeStatistics> rDict = GetChannelStatistics(lstChannelInfo);
        TopVideosList rVideoList = new(videoCount: 1000);

        GetChannelRecentViewStatistic(lstChannelInfo, ref rDict, ref rVideoList);

        return (rDict, rVideoList);
    }

    private List<Google.Apis.YouTube.v3.Data.Channel> GetChannelInfoList(List<string> lstChannelId)
    {
        List<Google.Apis.YouTube.v3.Data.Channel> rLst = new(lstChannelId.Count);

        // When querying multipie channel statistics, only 50 videos at a time is allowed
        List<string> lstIdRequest = Generate50IdsStringList(lstChannelId);

        foreach (string idRequest in lstIdRequest)
        {
            IList<Google.Apis.YouTube.v3.Data.Channel>? lstChannelInfo = GetChannelStatisticsResponse(idRequest);

            if (lstChannelInfo is not null)
                rLst.AddRange(lstChannelInfo);
        }

        return rLst;
    }

    private IList<Google.Apis.YouTube.v3.Data.Channel>? GetChannelStatisticsResponse(string idRequestString)
    {
        ChannelsResource.ListRequest channelsListRequest = youtubeService.Channels.List("statistics, contentDetails");
        channelsListRequest.Id = idRequestString;
        channelsListRequest.MaxResults = 50;

        IList<Google.Apis.YouTube.v3.Data.Channel>? responseItems = null;
        bool hasResponse = false;
        // try for three times
        for (int i = 0; i < 3; i++)
        {
            Google.Apis.YouTube.v3.Data.ChannelListResponse channelsListResponse = channelsListRequest.Execute();
            responseItems = channelsListResponse.Items;

            if (responseItems is null)
            {
                continue;
            }

            hasResponse = true;
            break;
        }

        if (!hasResponse)
        {
            return null;
        }

        return responseItems;
    }

    private static Dictionary<string, YouTubeStatistics> GetChannelStatistics(List<Google.Apis.YouTube.v3.Data.Channel> lstChannelInfo)
    {
        Dictionary<string, YouTubeStatistics> rDict = new(lstChannelInfo.Count);

        foreach (Google.Apis.YouTube.v3.Data.Channel channelInfo in lstChannelInfo)
        {
            // do not add channel if already in rDict
            if (rDict.ContainsKey(channelInfo.Id))
            {
                continue;
            }

            ulong? viewCount = channelInfo.Statistics.ViewCount;
            ulong? subscriberCount = channelInfo.Statistics.SubscriberCount;

            YouTubeStatistics statistics = new()
            {
                SubscriberCount = subscriberCount.GetValueOrDefault(0),
                ViewCount = viewCount.GetValueOrDefault(0),
            };

            rDict.Add(channelInfo.Id, statistics);
        }

        return rDict;
    }

    private Dictionary<string, DateTime> GetChannelRecentVideoList(Google.Apis.YouTube.v3.Data.Channel channelInfo)
    {
        Dictionary<string, DateTime> dictIdTime = new();

        string uploadsListId = channelInfo.ContentDetails.RelatedPlaylists.Uploads;
        string nextPageToken = "";
        while (nextPageToken != null)
        {
            PlaylistItemsResource.ListRequest playlistItemsListRequest = youtubeService.PlaylistItems.List("snippet,status");
            playlistItemsListRequest.PlaylistId = uploadsListId;
            playlistItemsListRequest.MaxResults = 1000;
            playlistItemsListRequest.PageToken = nextPageToken;

            // Retrieve the list of videos uploaded to the authenticated user's channel.
            Google.Apis.YouTube.v3.Data.PlaylistItemListResponse? playlistItemsListResponse;
            try
            {
                playlistItemsListResponse = playlistItemsListRequest.Execute();
            }
            catch
            {
                // return true because it is a successful result
                return new();
            }

            foreach (Google.Apis.YouTube.v3.Data.PlaylistItem playlistItem in playlistItemsListResponse.Items)
            {
                DateTime? videoPublishTime = playlistItem.Snippet.PublishedAt;
                string videoPrivacyStatus = playlistItem.Status.PrivacyStatus;
                string videoId = playlistItem.Snippet.ResourceId.VideoId;

                // only add video id if its publish time is within 30 days
                if (videoPublishTime is not null)
                {
                    DateTime publishTime = videoPublishTime.Value;
                    if ((DateTime.UtcNow - publishTime) < TimeSpan.FromDays(30))
                    {
                        dictIdTime.Add(videoId, videoPublishTime.Value);
                    }
                }
            }

            nextPageToken = playlistItemsListResponse.NextPageToken;
        }

        return dictIdTime;
    }

    private void GetChannelRecentViewStatistic(List<Google.Apis.YouTube.v3.Data.Channel> lstChannelInfo, ref Dictionary<string, YouTubeStatistics> dictIdStatistics, ref TopVideosList topVideosList)
    {
        foreach (Google.Apis.YouTube.v3.Data.Channel channelInfo in lstChannelInfo)
        {
            Dictionary<string, DateTime> dictIdTime = GetChannelRecentVideoList(channelInfo);

            // When querying multipie video statistics, only 50 videos at a time is allowed
            List<string> idRequestList = Generate50IdsStringList(dictIdTime.Keys.ToList());

            // The Tuple is video ID and video view count
            List<Tuple<string, ulong>> lstIdViewCount = new();
            foreach (string idRequestString in idRequestList)
            {
                VideosResource.ListRequest videosListRequest = youtubeService.Videos.List("id,snippet,statistics");
                videosListRequest.Id = idRequestString;

                Google.Apis.YouTube.v3.Data.VideoListResponse videoListResponse = videosListRequest.Execute();

                foreach (Google.Apis.YouTube.v3.Data.Video video in videoListResponse.Items)
                {
                    ulong? viewCount = video.Statistics.ViewCount;
                    if (viewCount is not null)
                    {
                        lstIdViewCount.Add(new Tuple<string, ulong>(video.Id, viewCount.Value));
                    }

                    topVideosList.Insert(new VideoInformation
                    {
                        Owner = channelInfo.Id,
                        Url = $"https://www.youtube.com/watch?v={video.Id}",
                        Title = video.Snippet.Title,
                        ThumbnailUrl = video.Snippet.Thumbnails.Medium.Url,
                        PublishDateTime = video.Snippet.PublishedAt.GetValueOrDefault(DateTime.UnixEpoch).ToUniversalTime(),
                        ViewCount = viewCount.GetValueOrDefault(),
                    });
                }
            }

            ulong medianViews = 0;
            ulong highestViews = 0;
            string highestViewedUrl = "";
            if (lstIdViewCount.Count != 0)
            {
                medianViews = GetMedian(lstIdViewCount);

                lstIdViewCount.Sort(CompareTupleSecondValue);
                Tuple<string, ulong> highestViewPair = lstIdViewCount.Last();
                highestViews = highestViewPair.Item2;
                highestViewedUrl = $"https://www.youtube.com/watch?v={highestViewPair.Item1}";
            }

            dictIdStatistics[channelInfo.Id].RecentMedianViewCount = medianViews;
            dictIdStatistics[channelInfo.Id].RecentHighestViewCount = highestViews;
            dictIdStatistics[channelInfo.Id].HighestViewedVideoURL = highestViewedUrl;
        }
    }

    private static List<string> Generate50IdsStringList(List<string> KeyList)
    {
        List<string> ans = new();

        int index;
        // pack 50 ids into a string
        for (index = 0; index < (KeyList.Count) / 50 * 50; index += 50)
        {
            string idRequestString = "";
            for (int offset = 0; offset < 50; offset++)
                idRequestString += KeyList[index + offset] + ',';
            idRequestString = idRequestString.Substring(0, idRequestString.Length - 1);
            ans.Add(idRequestString);
        }

        // residual
        if (KeyList.Count % 50 != 0)
        {
            string idRequestStringRes = "";
            for (; index < KeyList.Count; index++)
            {
                idRequestStringRes += KeyList[index] + ',';
            }
            idRequestStringRes = idRequestStringRes.Substring(0, idRequestStringRes.Length - 1);
            ans.Add(idRequestStringRes);
        }

        return ans;
    }

    private static ulong GetMedian(List<Tuple<string, ulong>> list)
    {
        list.Sort(CompareTupleSecondValue);

        if (list.Count == 0)
            return 0;

        if (list.Count == 1)
            return list[0].Item2;

        if (list.Count % 2 == 1)
            return list[list.Count / 2].Item2;
        else
            return (list[list.Count / 2 - 1].Item2 + list[list.Count / 2].Item2) / 2;
    }

    private static int CompareTupleSecondValue(Tuple<string, ulong> v1, Tuple<string, ulong> v2)
    {
        return Comparer<ulong>.Default.Compare(v1.Item2, v2.Item2);
    }
}