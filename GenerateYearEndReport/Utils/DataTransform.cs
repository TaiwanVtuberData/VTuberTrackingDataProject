﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GenerateRecordList.Types;

namespace GenerateYearEndReport.Utils;

public class DataTransform(DateTimeOffset latestRecordTime, DateTimeOffset latestBasicDataTime)
{
    private readonly DateTimeOffset LatestRecordTime = latestRecordTime;
    private readonly DateTimeOffset LatestBasicDataTime = latestBasicDataTime;

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

    public BaseCountType ToTwitchFollower(VTuberRecord.TwitchData? input)
    {
        if (input == null)
            return new NoCountType();

        VTuberRecord.TwitchData.BasicData? basicData = input.GetBasicDataOrLatest(
            LatestBasicDataTime
        );
        ulong? follower = basicData?.FollowerCount ?? null;

        return ToTwitchCountType(input.hasValidRecord, follower);
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

    public ulong ToYouTubeTotalViewCount(VTuberRecord.YouTubeData? input)
    {
        if (input == null)
            return 0;

        VTuberRecord.YouTubeData.BasicData? basicData = input.GetBasicDataOrLatest(
            LatestBasicDataTime
        );

        return basicData?.TotalViewCount ?? 0;
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
