using Common.Types;
using Common.Types.Basic;
using Common.Utils;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using log4net;
using System.Collections.Immutable;
using System.Net;

namespace FetchYouTubeStatistics;
public class Fetcher {
    private static readonly ILog log = LogManager.GetLogger(typeof(Fetcher));
    public string ApiKey { get; set; }
    private readonly YouTubeService youtubeService;
    private readonly DateTimeOffset CurrentTime;

    public Fetcher(string ApiKey, DateTimeOffset currentTime) {
        this.ApiKey = ApiKey;
        youtubeService = new YouTubeService(new BaseClientService.Initializer() { ApiKey = this.ApiKey });

        CurrentTime = currentTime;
    }

    public (ImmutableDictionary<YouTubeChannelId, YouTubeRecord>, TopVideosList, LiveVideosList) GetAll(IImmutableList<YouTubeChannelId> lstChannelId) {
        ImmutableDictionary<YouTubeChannelId, Google.Apis.YouTube.v3.Data.Channel> dictChannelInfo = GetChannelInfoList(lstChannelId);

        ImmutableDictionary<YouTubeChannelId, YouTubeRecord.BasicRecord> dictBasicRecord = GetChannelBasicRecord(dictChannelInfo);

        ChannelRecentViewRecord channelRecentViewRecord = GetChannelRecentViewRecord(dictChannelInfo);

        ImmutableDictionary<YouTubeChannelId, YouTubeRecord> rDict = CreateYouTubeRecord(dictBasicRecord, channelRecentViewRecord.DictRecentRecordTuple);

        return (rDict, channelRecentViewRecord.TopVideosList, channelRecentViewRecord.LiveVideosList);
    }

    private static ImmutableDictionary<YouTubeChannelId, YouTubeRecord> CreateYouTubeRecord(
        IImmutableDictionary<YouTubeChannelId, YouTubeRecord.BasicRecord> dictBasicRecord,
        IImmutableDictionary<YouTubeChannelId, YouTubeRecord.RecentRecordTuple> dictRecentRecordTuple) {
        Dictionary<YouTubeChannelId, YouTubeRecord> rDict = new(dictBasicRecord.Count);

        foreach (KeyValuePair<YouTubeChannelId, YouTubeRecord.BasicRecord> keyValuePair in dictBasicRecord) {
            YouTubeChannelId channelId = keyValuePair.Key;
            YouTubeRecord.BasicRecord basicRecord = keyValuePair.Value;
            dictRecentRecordTuple.TryGetValue(channelId, out YouTubeRecord.RecentRecordTuple? recentRecordTuple);

            if (recentRecordTuple is null) {
                continue;
            }

            rDict.Add(channelId, new YouTubeRecord(basicRecord, recentRecordTuple));
        }

        return rDict.ToImmutableDictionary();
    }

    private ImmutableDictionary<YouTubeChannelId, Google.Apis.YouTube.v3.Data.Channel> GetChannelInfoList(IImmutableList<YouTubeChannelId> lstChannelId) {
        Dictionary<YouTubeChannelId, Google.Apis.YouTube.v3.Data.Channel> rDict = new(lstChannelId.Count);

        // When querying multipie channel statistics, only 50 videos at a time is allowed
        ImmutableList<IdRequstString> lstIdRequest = Generate50IdsStringList(lstChannelId.Map(e => e.Value).ToImmutableList());

        foreach (IdRequstString idRequest in lstIdRequest) {
            ImmutableList<Google.Apis.YouTube.v3.Data.Channel>? lstChannelInfo = GetChannelStatisticsResponse(idRequest);

            if (lstChannelInfo is null) {
                continue;
            }

            foreach (Google.Apis.YouTube.v3.Data.Channel channelInfo in lstChannelInfo) {
                rDict.TryAdd(new YouTubeChannelId(channelInfo.Id), channelInfo);
            }
        }

        return rDict.ToImmutableDictionary();
    }

    private ImmutableList<Google.Apis.YouTube.v3.Data.Channel>? GetChannelStatisticsResponse(IdRequstString idRequestString) {
        ChannelsResource.ListRequest channelsListRequest = youtubeService.Channels.List("statistics, contentDetails");
        channelsListRequest.Id = idRequestString.Value;
        channelsListRequest.MaxResults = 50;

        Google.Apis.YouTube.v3.Data.ChannelListResponse? channelsListResponse = ExecuteYouTubeThrowableWithRetry(() => channelsListRequest.Execute());

        // channellistItemsListResponse.Items is actually nullable
        return channelsListResponse?.Items?.ToImmutableList();
    }

    private static ImmutableDictionary<YouTubeChannelId, YouTubeRecord.BasicRecord> GetChannelBasicRecord(
        IImmutableDictionary<YouTubeChannelId, Google.Apis.YouTube.v3.Data.Channel> dictChannelInfo
        ) => dictChannelInfo.ToImmutableDictionary(
            keyValuePair => keyValuePair.Key,
            keyValuePair => new YouTubeRecord.BasicRecord(
                SubscriberCount: keyValuePair.Value.Statistics.SubscriberCount ?? 0,
                ViewCount: keyValuePair.Value.Statistics.ViewCount ?? 0
                )
            );

    private ImmutableDictionary<YouTubeVideoId, DateTimeOffset> GetChannelRecentVideoList(Google.Apis.YouTube.v3.Data.Channel channelInfo) {
        Dictionary<YouTubeVideoId, DateTimeOffset> rDict = [];
        int DAYS_LIMIT = 90;

        string uploadsListId = channelInfo.ContentDetails.RelatedPlaylists.Uploads;
        string nextPageToken = "";
        while (nextPageToken != null) {
            PlaylistItemsResource.ListRequest playlistItemsListRequest = youtubeService.PlaylistItems.List("snippet,status");
            playlistItemsListRequest.PlaylistId = uploadsListId;
            playlistItemsListRequest.MaxResults = 1000;
            playlistItemsListRequest.PageToken = nextPageToken;

            // Retrieve the list of videos uploaded to the user's channel.

            Google.Apis.YouTube.v3.Data.PlaylistItemListResponse? playlistItemsListResponse = ExecuteYouTubeThrowableWithRetry(() => playlistItemsListRequest.Execute());
            if (playlistItemsListResponse == null) {
                // return true because it is a successful result
                return ImmutableDictionary<YouTubeVideoId, DateTimeOffset>.Empty;
            }

            foreach (Google.Apis.YouTube.v3.Data.PlaylistItem playlistItem in playlistItemsListResponse.Items) {
                DateTimeOffset? videoPublishTime = playlistItem.Snippet.PublishedAtDateTimeOffset;
                string videoId = playlistItem.Snippet.ResourceId.VideoId;

                // only add video id if its publish time is within DAYS_LIMIT days
                if (videoPublishTime is not null) {
                    if ((CurrentTime - videoPublishTime.Value) < TimeSpan.FromDays(DAYS_LIMIT)) {
                        rDict.Add(new YouTubeVideoId(videoId), videoPublishTime.Value);
                    }
                }
            }

            nextPageToken = playlistItemsListResponse.NextPageToken;
        }

        return rDict.ToImmutableDictionary();
    }

    private ChannelRecentViewRecord GetChannelRecentViewRecord(
        ImmutableDictionary<YouTubeChannelId, Google.Apis.YouTube.v3.Data.Channel> dictChannelInfo) {
        Dictionary<YouTubeChannelId, YouTubeRecord.RecentRecordTuple> rDict = [];
        TopVideosList rTopVideosList = new();
        LiveVideosList rLiveVideosList = [];

        foreach (KeyValuePair<YouTubeChannelId, Google.Apis.YouTube.v3.Data.Channel> keyValuePair in dictChannelInfo) {
            YouTubeChannelId channelId = keyValuePair.Key;
            Google.Apis.YouTube.v3.Data.Channel channelInfo = keyValuePair.Value;

            ImmutableDictionary<YouTubeVideoId, DateTimeOffset> dictVideoIdAndTime = GetChannelRecentVideoList(channelInfo);

            // When querying multipie video statistics, only 50 videos at a time is allowed
            ImmutableList<IdRequstString> lstIdRequest = Generate50IdsStringList(dictVideoIdAndTime.Keys.Map(e => e.Value).ToImmutableList());

            // The Tuple is video ID and video view count
            List<Tuple<DateTimeOffset, YouTubeVideoId, ulong>> lstTotalViewCount = [];
            List<Tuple<DateTimeOffset, YouTubeVideoId, ulong>> lstLivestreamViewCount = [];
            List<Tuple<DateTimeOffset, YouTubeVideoId, ulong>> lstVideoViewCount = [];
            foreach (IdRequstString idRequst in lstIdRequest) {
                VideosResource.ListRequest videosListRequest = youtubeService.Videos.List("id,snippet,statistics,liveStreamingDetails");
                videosListRequest.Id = idRequst.Value;

                Google.Apis.YouTube.v3.Data.VideoListResponse? videoListResponse = ExecuteYouTubeThrowableWithRetry(() => videosListRequest.Execute());

                if (videoListResponse == null) {
                    continue;
                }

                foreach (Google.Apis.YouTube.v3.Data.Video video in videoListResponse.Items) {
                    ulong? viewCount = video.Statistics.ViewCount;
                    DateTimeOffset? publishTime = video.Snippet.PublishedAtDateTimeOffset;

                    // if there is view count and the video is not (streaming or upcoming livestream) and 
                    if (viewCount is not null
                        && publishTime is not null
                        && (CurrentTime - publishTime) < TimeSpan.FromDays(30)
                        && !LiveVideoTypeConvert.IsLiveVideoType(video.Snippet.LiveBroadcastContent)) {
                        lstTotalViewCount.Add(new(publishTime ?? DateTimeOffset.UnixEpoch, new YouTubeVideoId(video.Id), viewCount.Value));

                        bool isLivestream = video.LiveStreamingDetails is not null;
                        bool isLongerThan15Minutes = false;
                        if (isLivestream) {
                            DateTimeOffset? startTime = video.LiveStreamingDetails?.ActualStartTimeDateTimeOffset;
                            DateTimeOffset? endTime = video.LiveStreamingDetails?.ActualEndTimeDateTimeOffset;

                            if (startTime is not null && endTime is not null) {
                                TimeSpan duration = endTime.Value - startTime.Value;
                                if (duration > TimeSpan.FromMinutes(15)) {
                                    isLongerThan15Minutes = true;
                                }
                            }
                        }

                        if (isLivestream && isLongerThan15Minutes) {
                            lstLivestreamViewCount.Add(new(publishTime ?? DateTimeOffset.UnixEpoch, new YouTubeVideoId(video.Id), viewCount.Value));
                        } else {
                            lstVideoViewCount.Add(new(publishTime ?? DateTimeOffset.UnixEpoch, new YouTubeVideoId(video.Id), viewCount.Value));
                        }

                        rTopVideosList.Insert(new VideoInformation {
                            Id = new VTuberId(channelInfo.Id),
                            Url = $"https://www.youtube.com/watch?v={video.Id}",
                            Title = video.Snippet.Title,
                            ThumbnailUrl = video.Snippet.Thumbnails.Medium.Url,
                            PublishDateTime = publishTime ?? DateTimeOffset.UnixEpoch,
                            ViewCount = viewCount ?? 0,
                        });
                    }

                    if (LiveVideoTypeConvert.IsLiveVideoType(video.Snippet.LiveBroadcastContent)) {
                        DateTimeOffset startTime =
                            (video.LiveStreamingDetails?.ActualStartTimeDateTimeOffset
                            ?? video.LiveStreamingDetails?.ScheduledStartTimeDateTimeOffset
                            ?? DateTimeOffset.UnixEpoch);

                        rLiveVideosList.Add(new LiveVideoInformation {
                            Id = new VTuberId(channelInfo.Id),
                            Url = $"https://www.youtube.com/watch?v={video.Id}",
                            Title = video.Snippet.Title,
                            ThumbnailUrl = video.Snippet.Thumbnails.Medium.Url,
                            PublishDateTime = startTime,
                            VideoType = LiveVideoTypeConvert.FromString(video.Snippet.LiveBroadcastContent),
                        });
                    }
                }
            }

            YouTubeRecord.RecentRecord recentTotalRecord = CreateRecentRecord(lstTotalViewCount.ToImmutableList(), CurrentTime);
            YouTubeRecord.RecentRecord recentLivestreamRecord = CreateRecentRecord(lstLivestreamViewCount.ToImmutableList(), CurrentTime);
            YouTubeRecord.RecentRecord recentVideoRecord = CreateRecentRecord(lstVideoViewCount.ToImmutableList(), CurrentTime);

            rDict.Add(channelId, new YouTubeRecord.RecentRecordTuple(
                Total: recentTotalRecord,
                Livestream: recentLivestreamRecord,
                Video: recentVideoRecord
                )
                );
        }

        return new ChannelRecentViewRecord(
            DictRecentRecordTuple: rDict.ToImmutableDictionary(),
            TopVideosList: rTopVideosList,
            rLiveVideosList
            );
    }

    private static YouTubeRecord.RecentRecord CreateRecentRecord(
        IImmutableList<Tuple<DateTimeOffset, YouTubeVideoId, ulong>> lstIdViewCount,
        DateTimeOffset currentTime) {
        ulong medianViews = 0;
        ulong popularity = 0;
        ulong highestViews = 0;
        string highestViewedUrl = "";
        if (lstIdViewCount.Count != 0) {
            ImmutableList<Tuple<DateTimeOffset, string, ulong>> lstIdViewCountTemp = lstIdViewCount.Map(e =>
                new Tuple<DateTimeOffset, string, ulong>(e.Item1, e.Item2.Value, e.Item3)
                ).ToImmutableList();

            medianViews = NumericUtility.GetMedian(lstIdViewCountTemp);
            popularity = (ulong)NumericUtility.GetPopularity(lstIdViewCountTemp, currentTime.UtcDateTime);
            Tuple<DateTimeOffset, string, ulong> largest = NumericUtility.GetLargest(lstIdViewCountTemp);
            highestViews = largest.Item3;
            highestViewedUrl = $"https://www.youtube.com/watch?v={largest.Item2}";
        }

        return new(medianViews, popularity, highestViews, highestViewedUrl);
    }

    private static ImmutableList<IdRequstString> Generate50IdsStringList(IImmutableList<string> KeyList) {
        List<IdRequstString> rLst = [];

        int index;
        // pack 50 ids into a string
        for (index = 0; index < (KeyList.Count) / 50 * 50; index += 50) {
            string idRequestString = "";
            for (int offset = 0; offset < 50; offset++)
                idRequestString += KeyList[index + offset] + ',';
            idRequestString = idRequestString.Substring(0, idRequestString.Length - 1);
            rLst.Add(new IdRequstString(idRequestString));
        }

        // residual
        if (KeyList.Count % 50 != 0) {
            string idRequestStringRes = "";
            for (; index < KeyList.Count; index++) {
                idRequestStringRes += KeyList[index] + ',';
            }
            idRequestStringRes = idRequestStringRes.Substring(0, idRequestStringRes.Length - 1);
            rLst.Add(new IdRequstString(idRequestStringRes));
        }

        return rLst.ToImmutableList();
    }

    private static T? ExecuteYouTubeThrowableWithRetry<T>(Func<T> func) where T : class? {
        int RETRY_TIME = 10;
        TimeSpan RETRY_DELAY = new(hours: 0, minutes: 0, seconds: 3);

        for (int i = 0; i < RETRY_TIME; i++) {
            try {
                return func.Invoke();
            } catch (Google.GoogleApiException e) {
                if (e.HttpStatusCode == HttpStatusCode.NotFound) {
                    log.Warn($"Request HttpStatusCode is HttpStatusCode.NotFound.");
                    return null;
                }
            } catch (Exception e) {
                log.Warn($"Failed to execute {func.Method.Name}. {i} tries. Retry after {RETRY_DELAY.TotalSeconds} seconds.");
                log.Warn(e.Message, e);
                Task.Delay(RETRY_DELAY);
            }
        }

        return null;
    }
}