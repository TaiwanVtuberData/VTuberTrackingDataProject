﻿using GenerateRecordList.Types;

namespace GenerateRecordList.Utils;

public class DataTransform(DateTimeOffset latestRecordTime, DateTimeOffset latestBasicDataTime)
{
    private readonly DateTimeOffset LatestRecordTime = latestRecordTime;
    private readonly DateTimeOffset LatestBasicDataTime = latestBasicDataTime;

    public YouTubeData? ToYouTubeData(VTuberRecord.YouTubeData? input)
    {
        if (input == null)
            return null;

        VTuberRecord.YouTubeData.BasicData? basicData = input.GetBasicDataOrLatest(
            LatestBasicDataTime
        );
        ulong? sub = basicData?.SubscriberCount;

        return new YouTubeData(
            id: input.ChannelId,
            subscriber: ToYouTubeCountType(input.hasValidRecord, sub)
        );
    }

    public YouTubePopularityData? ToYouTubeTotalPopularityData(VTuberRecord.YouTubeData? input)
    {
        if (input == null)
            return null;

        VTuberRecord.YouTubeData.BasicData? basicData = input.GetBasicDataOrLatest(
            LatestBasicDataTime
        );
        ulong? sub = basicData?.SubscriberCount;

        VTuberRecord.YouTubeData.Record? record = input.GetRecordOrLatest(LatestRecordTime);

        return new YouTubePopularityData(
            id: input.ChannelId,
            subscriber: ToYouTubeCountType(input.hasValidRecord, sub),
            popularity: record?.RecentTotalMedianViewCount ?? 0
        );
    }

    public YouTubePopularityData? ToYouTubeLivestreamPopularityData(VTuberRecord.YouTubeData? input)
    {
        if (input == null)
            return null;

        VTuberRecord.YouTubeData.BasicData? basicData = input.GetBasicDataOrLatest(
            LatestBasicDataTime
        );
        ulong? sub = basicData?.SubscriberCount;

        VTuberRecord.YouTubeData.Record? record = input.GetRecordOrLatest(LatestRecordTime);

        return new YouTubePopularityData(
            id: input.ChannelId,
            subscriber: ToYouTubeCountType(input.hasValidRecord, sub),
            popularity: record?.RecentLivestreamMedianViewCount ?? 0
        );
    }

    public YouTubePopularityData? ToYouTubeVideoPopularityData(VTuberRecord.YouTubeData? input)
    {
        if (input == null)
            return null;

        VTuberRecord.YouTubeData.BasicData? basicData = input.GetBasicDataOrLatest(
            LatestBasicDataTime
        );
        ulong? sub = basicData?.SubscriberCount;

        VTuberRecord.YouTubeData.Record? record = input.GetRecordOrLatest(LatestRecordTime);

        return new YouTubePopularityData(
            id: input.ChannelId,
            subscriber: ToYouTubeCountType(input.hasValidRecord, sub),
            popularity: record?.RecentVideoMedianViewCount ?? 0
        );
    }

    public YouTubeSubscriberCountToPopularityData? ToYouTubeSubscriberCountToLivestreamPopularity(
        VTuberRecord.YouTubeData? input
    )
    {
        if (input == null)
            return null;

        VTuberRecord.YouTubeData.BasicData? basicData = input.GetBasicDataOrLatest(
            LatestBasicDataTime
        );
        ulong? sub = basicData?.SubscriberCount;

        VTuberRecord.YouTubeData.Record? record = input.GetRecordOrLatest(LatestRecordTime);

        decimal subscriberCountToPopularity =
            (sub == null || sub == 0)
                ? 0m
                : (record?.RecentLivestreamMedianViewCount ?? 0) / (decimal)sub;

        return new YouTubeSubscriberCountToPopularityData(
            id: input.ChannelId,
            subscriber: ToYouTubeCountType(input.hasValidRecord, sub),
            popularity: subscriberCountToPopularity * 100
        );
    }

    public YouTubeSubscriberCountToPopularityData? ToYouTubeSubscriberCountToVideoPopularity(
        VTuberRecord.YouTubeData? input
    )
    {
        if (input == null)
            return null;

        VTuberRecord.YouTubeData.BasicData? basicData = input.GetBasicDataOrLatest(
            LatestBasicDataTime
        );
        ulong? sub = basicData?.SubscriberCount;

        VTuberRecord.YouTubeData.Record? record = input.GetRecordOrLatest(LatestRecordTime);

        decimal subscriberCountToPopularity =
            (sub == null || sub == 0)
                ? 0m
                : (record?.RecentVideoMedianViewCount ?? 0) / (decimal)sub;

        return new YouTubeSubscriberCountToPopularityData(
            id: input.ChannelId,
            subscriber: ToYouTubeCountType(input.hasValidRecord, sub),
            popularity: subscriberCountToPopularity * 100
        );
    }

    public YouTubeSubscriberCountToPopularityData? ToYouTubeSubscriberCountToTotalPopularity(
        VTuberRecord.YouTubeData? input
    )
    {
        if (input == null)
            return null;

        VTuberRecord.YouTubeData.BasicData? basicData = input.GetBasicDataOrLatest(
            LatestBasicDataTime
        );
        ulong? sub = basicData?.SubscriberCount;

        VTuberRecord.YouTubeData.Record? record = input.GetRecordOrLatest(LatestRecordTime);

        decimal subscriberCountToPopularity =
            (sub == null || sub == 0)
                ? 0m
                : (record?.RecentTotalMedianViewCount ?? 0) / (decimal)sub;

        return new YouTubeSubscriberCountToPopularityData(
            id: input.ChannelId,
            subscriber: ToYouTubeCountType(input.hasValidRecord, sub),
            popularity: subscriberCountToPopularity * 100
        );
    }

    public ulong ToYouTubeTotalPopularity(VTuberRecord.YouTubeData? input)
    {
        if (input == null)
            return 0;

        VTuberRecord.YouTubeData.Record? record = input.GetRecordOrLatest(LatestRecordTime);

        return record?.RecentTotalMedianViewCount ?? 0;
    }

    public ulong ToYouTubeLivestreamPopularity(VTuberRecord.YouTubeData? input)
    {
        if (input == null)
            return 0;

        VTuberRecord.YouTubeData.Record? record = input.GetRecordOrLatest(LatestRecordTime);

        return record?.RecentLivestreamMedianViewCount ?? 0;
    }

    public ulong ToYouTubeVideoPopularity(VTuberRecord.YouTubeData? input)
    {
        if (input == null)
            return 0;

        VTuberRecord.YouTubeData.Record? record = input.GetRecordOrLatest(LatestRecordTime);

        return record?.RecentVideoMedianViewCount ?? 0;
    }

    public BaseCountType ToYouTubeSubscriber(VTuberRecord.YouTubeData? input)
    {
        if (input == null)
            return new NoCountType();

        VTuberRecord.YouTubeData.BasicData? basicData = input.GetBasicDataOrLatest(
            LatestBasicDataTime
        );
        ulong? sub = basicData?.SubscriberCount ?? null;

        return ToYouTubeCountType(input.hasValidRecord, sub);
    }

    public ulong ToYouTubeTotalViewCount(VTuberRecord.YouTubeData? input)
    {
        if (input == null)
            return 0;

        VTuberRecord.YouTubeData.BasicData? basicData = input.GetBasicDataOrLatest(
            LatestBasicDataTime
        );

        return basicData?.TotalViewCount ?? 0;
    }

    public TwitchData? ToTwitchData(VTuberRecord.TwitchData? input)
    {
        if (input == null)
            return null;

        VTuberRecord.TwitchData.BasicData? basicData = input.GetBasicDataOrLatest(
            LatestBasicDataTime
        );
        ulong? follower = basicData?.FollowerCount;

        return new TwitchData(
            id: input.ChannelName,
            follower: ToTwitchCountType(input.hasValidRecord, follower)
        );
    }

    public TwitchPopularityData? ToTwitchPopularityData(VTuberRecord.TwitchData? input)
    {
        if (input == null)
            return null;

        VTuberRecord.TwitchData.BasicData? basicData = input.GetBasicDataOrLatest(
            LatestBasicDataTime
        );
        ulong? follower = basicData?.FollowerCount;

        VTuberRecord.TwitchData.Record? record = input.GetRecordOrLatest(LatestRecordTime);
        ulong popularity = record?.RecentMedianViewCount ?? 0;

        return new TwitchPopularityData(
            id: input.ChannelName,
            follower: ToTwitchCountType(input.hasValidRecord, follower),
            popularity: popularity
        );
    }

    public ulong ToTwitchPopularity(VTuberRecord.TwitchData? input)
    {
        if (input == null)
            return 0;

        VTuberRecord.TwitchData.Record? record = input.GetRecordOrLatest(LatestRecordTime);

        return record?.RecentMedianViewCount ?? 0;
    }

    public ulong ToCombinedTotalPopularity(VTuberRecord? input)
    {
        if (input == null)
            return 0;

        return ToYouTubeTotalPopularity(input.YouTube) + ToTwitchPopularity(input.Twitch);
    }

    public ulong ToCombinedLivestreamPopularity(VTuberRecord? input)
    {
        if (input == null)
            return 0;

        return ToYouTubeLivestreamPopularity(input.YouTube) + ToTwitchPopularity(input.Twitch);
    }

    public ulong ToCombinedVideoPopularity(VTuberRecord? input)
    {
        if (input == null)
            return 0;

        return ToYouTubeVideoPopularity(input.YouTube);
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
            VTuberRecord.YouTubeData.Record? YTRecord = vtuberRecord.YouTube.GetRecordOrLatest(
                LatestRecordTime
            );

            if (YTRecord != null)
            {
                YouTubeVideoViewCount = YTRecord.HighestViewCount;
                YouTubeVideoId = YTRecord.HighestViewedVideoId;
            }
        }

        if (vtuberRecord.Twitch != null)
        {
            VTuberRecord.TwitchData.Record? TwitchRecord = vtuberRecord.Twitch.GetRecordOrLatest(
                LatestRecordTime
            );

            if (TwitchRecord != null)
            {
                TwitchVideoViewCount = TwitchRecord.HighestViewCount;
                TwitchVideoId = TwitchRecord.HighestViewedVideoId;
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

    private static BaseCountType ToYouTubeCountType(bool hasValidRecord, ulong? subCount)
    {
        if (subCount.HasValue && hasValidRecord)
        {
            if (subCount == 0)
                return new HiddenCountType();

            return new HasCountType(_count: subCount.Value);
        }

        return new NoCountType();
    }

    private static BaseCountType ToTwitchCountType(bool hasValidRecord, ulong? followerCount)
    {
        if (followerCount.HasValue && hasValidRecord)
        {
            return new HasCountType(_count: followerCount.Value);
        }

        return new NoCountType();
    }
}
