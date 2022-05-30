using GenerateJsonFile.Types;

namespace GenerateJsonFile.Utils;
internal class DataTransform
{
    private readonly DateTime LatestRecordTime;

    public DataTransform(DateTime latestRecordTime)
    {
        LatestRecordTime = latestRecordTime;
    }

    public YouTubeData? ToYouTubeData(VTuberRecord.YouTubeData? input)
    {
        if (input == null)
            return null;

        VTuberRecord.YouTubeData.YouTubeRecord? record = input.GetRecord(LatestRecordTime);
        ulong? sub = record.HasValue ? record.Value.SubscriberCount : null;

        return new YouTubeData(
            id: input.ChannelId,
            subscriber: ToYouTubeCountType(sub));
    }

    public YouTubePopularityData? ToYouTubePopularityData(VTuberRecord.YouTubeData? input)
    {
        if (input == null)
            return null;

        VTuberRecord.YouTubeData.YouTubeRecord? record = input.GetRecord(LatestRecordTime);
        ulong? sub = record.HasValue ? record.Value.SubscriberCount : null;
        ulong popularity = record.HasValue ? record.Value.RecentMedianViewCount : 0;

        return new YouTubePopularityData(
            id: input.ChannelId,
            subscriber: ToYouTubeCountType(sub),
            popularity: popularity);
    }
    public ulong ToYouTubePopularity(VTuberRecord.YouTubeData? input)
    {
        if (input == null)
            return 0;

        VTuberRecord.YouTubeData.YouTubeRecord? record = input.GetRecord(LatestRecordTime);

        return record.HasValue ? record.Value.RecentMedianViewCount : 0;
    }

    public BaseCountType ToYouTubeSubscriber(VTuberRecord.YouTubeData? input)
    {
        if (input == null)
            return new NoCountType();

        VTuberRecord.YouTubeData.YouTubeRecord? record = input.GetRecord(LatestRecordTime);
        ulong? sub = record.HasValue ? record.Value.SubscriberCount : null;

        return ToYouTubeCountType(sub);
    }

    public ulong ToYouTubeTotalViewCount(VTuberRecord.YouTubeData? input)
    {
        if (input == null)
            return 0;

        VTuberRecord.YouTubeData.YouTubeRecord? record = input.GetRecord(LatestRecordTime);

        return record.HasValue ? record.Value.TotalViewCount : 0;
    }

    public TwitchData? ToTwitchData(VTuberRecord.TwitchData? input)
    {
        if (input == null)
            return null;


        VTuberRecord.TwitchData.TwitchRecord? record = input.GetRecord(LatestRecordTime);
        ulong? follower = record.HasValue ? record.Value.FollowerCount : null;

        return new TwitchData(
            id: input.ChannelName,
            follower: ToTwitchCountType(follower));
    }

    public TwitchPopularityData? ToTwitchPopularityData(VTuberRecord.TwitchData? input)
    {
        if (input == null)
            return null;

        VTuberRecord.TwitchData.TwitchRecord? record = input.GetRecord(LatestRecordTime);
        ulong? follower = record.HasValue ? record.Value.FollowerCount : null;
        ulong popularity = record.HasValue ? record.Value.RecentMedianViewCount : 0;

        return new TwitchPopularityData(
            id: input.ChannelId,
            follower: ToTwitchCountType(follower),
            popularity: popularity);

    }
    public ulong ToTwitchPopularity(VTuberRecord.TwitchData? input)
    {
        if (input == null)
            return 0;

        VTuberRecord.TwitchData.TwitchRecord? record = input.GetRecord(LatestRecordTime);

        return record.HasValue ? record.Value.RecentMedianViewCount : 0;
    }

    public ulong ToCombinedPopularity(VTuberRecord? input)
    {
        if (input == null)
            return 0;

        return ToYouTubePopularity(input.YouTube) + ToTwitchPopularity(input.Twitch);
    }

    public VideoInfo? GetPopularVideo(VTuberRecord vtuberRecord)
    {
        if (vtuberRecord.YouTube == null && vtuberRecord.Twitch == null)
        {
            return null;
        }

        ulong YouTubeVideoViewCount = 0;
        ulong TwitchVideoViewCount = 0;
        string YouTubeVideoId = "";
        string TwitchVideoId = "";

        if (vtuberRecord.YouTube != null)
        {
            VTuberRecord.YouTubeData.YouTubeRecord? YTRecord = vtuberRecord.YouTube.GetRecord(LatestRecordTime);

            if (YTRecord != null)
            {
                YouTubeVideoViewCount = YTRecord.Value.HighestViewCount;
                YouTubeVideoId = YTRecord.Value.HighestViewedVideoId;
            }
        }


        if (vtuberRecord.Twitch != null)
        {
            VTuberRecord.TwitchData.TwitchRecord? TwitchRecord = vtuberRecord.Twitch.GetRecord(LatestRecordTime);

            if (TwitchRecord != null)
            {
                TwitchVideoViewCount = TwitchRecord.Value.HighestViewCount;
                TwitchVideoId = TwitchRecord.Value.HighestViewedVideoId;
            }
        }

        if (YouTubeVideoViewCount == 0 && TwitchVideoViewCount == 0)
        {
            return null;
        }

        if (YouTubeVideoViewCount > TwitchVideoViewCount)
        {
            return new VideoInfo(type: VideoType.YouTube, id: YouTubeVideoId);
        }
        else
        {
            return new VideoInfo(type: VideoType.Twitch, id: TwitchVideoId);
        }
    }

    private static BaseCountType ToYouTubeCountType(ulong? subCount)
    {
        if (subCount.HasValue)
        {
            if (subCount == 0)
                return new HiddenCountType();

            return new HasCountType(count: subCount.Value);
        }

        return new NoCountType();
    }

    private static BaseCountType ToTwitchCountType(ulong? followerCount)
    {
        if (followerCount.HasValue)
        {
            return new HasCountType(count: followerCount.Value);
        }

        return new NoCountType();
    }
}
