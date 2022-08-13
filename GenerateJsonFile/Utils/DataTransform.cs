using GenerateJsonFile.Types;

namespace GenerateJsonFile.Utils;
internal class DataTransform {
  private readonly DateTime LatestRecordTime;
  private readonly DateTime LatestBasicDataTime;

  public DataTransform(DateTime latestRecordTime, DateTime latestBasicDataTime) {
    LatestRecordTime = latestRecordTime;
    LatestBasicDataTime = latestBasicDataTime;
  }

  public YouTubeData? ToYouTubeData(VTuberRecord.YouTubeData? input) {
    if (input == null)
      return null;

    VTuberRecord.YouTubeData.BasicData? basicData = input.GetBasicData(LatestBasicDataTime);
    ulong? sub = basicData.HasValue ? basicData.Value.SubscriberCount : null;

    return new YouTubeData(
        id: input.ChannelId,
        subscriber: ToYouTubeCountType(input.hasValidRecord, sub));
  }

  public YouTubePopularityData? ToYouTubePopularityData(VTuberRecord.YouTubeData? input) {
    if (input == null)
      return null;

    VTuberRecord.YouTubeData.BasicData? basicData = input.GetBasicData(LatestBasicDataTime);
    ulong? sub = basicData.HasValue ? basicData.Value.SubscriberCount : null;

    VTuberRecord.YouTubeData.Record? record = input.GetRecord(LatestRecordTime);
    ulong popularity = record.HasValue ? record.Value.RecentMedianViewCount : 0;

    return new YouTubePopularityData(
        id: input.ChannelId,
        subscriber: ToYouTubeCountType(input.hasValidRecord, sub),
        popularity: popularity);
  }
  public ulong ToYouTubePopularity(VTuberRecord.YouTubeData? input) {
    if (input == null)
      return 0;

    VTuberRecord.YouTubeData.Record? record = input.GetRecord(LatestRecordTime);

    return record.HasValue ? record.Value.RecentMedianViewCount : 0;
  }

  public BaseCountType ToYouTubeSubscriber(VTuberRecord.YouTubeData? input) {
    if (input == null)
      return new NoCountType();

    VTuberRecord.YouTubeData.BasicData? basicData = input.GetBasicData(LatestBasicDataTime);
    ulong? sub = basicData.HasValue ? basicData.Value.SubscriberCount : null;

    return ToYouTubeCountType(input.hasValidRecord, sub);
  }

  public ulong ToYouTubeTotalViewCount(VTuberRecord.YouTubeData? input) {
    if (input == null)
      return 0;

    VTuberRecord.YouTubeData.BasicData? basicData = input.GetBasicData(LatestBasicDataTime);

    return basicData.HasValue ? basicData.Value.TotalViewCount : 0;
  }

  public TwitchData? ToTwitchData(VTuberRecord.TwitchData? input) {
    if (input == null)
      return null;


    VTuberRecord.TwitchData.BasicData? basicData = input.GetBasicData(LatestBasicDataTime);
    ulong? follower = basicData.HasValue ? basicData.Value.FollowerCount : null;

    return new TwitchData(
        id: input.ChannelName,
        follower: ToTwitchCountType(input.hasValidRecord, follower));
  }

  public TwitchPopularityData? ToTwitchPopularityData(VTuberRecord.TwitchData? input) {
    if (input == null)
      return null;

    VTuberRecord.TwitchData.BasicData? basicData = input.GetBasicData(LatestBasicDataTime);
    ulong? follower = basicData.HasValue ? basicData.Value.FollowerCount : null;

    VTuberRecord.TwitchData.Record? record = input.GetRecord(LatestRecordTime);
    ulong popularity = record.HasValue ? record.Value.RecentMedianViewCount : 0;

    return new TwitchPopularityData(
        id: input.ChannelId,
        follower: ToTwitchCountType(input.hasValidRecord, follower),
        popularity: popularity);

  }
  public ulong ToTwitchPopularity(VTuberRecord.TwitchData? input) {
    if (input == null)
      return 0;

    VTuberRecord.TwitchData.Record? record = input.GetRecord(LatestRecordTime);

    return record.HasValue ? record.Value.RecentMedianViewCount : 0;
  }

  public ulong ToCombinedPopularity(VTuberRecord? input) {
    if (input == null)
      return 0;

    return ToYouTubePopularity(input.YouTube) + ToTwitchPopularity(input.Twitch);
  }

  public VideoInfo? GetPopularVideo(VTuberRecord vtuberRecord) {
    if (vtuberRecord.YouTube == null && vtuberRecord.Twitch == null) {
      return null;
    }

    ulong YouTubeVideoViewCount = 0;
    ulong TwitchVideoViewCount = 0;
    string YouTubeVideoId = "";
    string TwitchVideoId = "";

    if (vtuberRecord.YouTube != null) {
      VTuberRecord.YouTubeData.Record? YTRecord = vtuberRecord.YouTube.GetRecord(LatestRecordTime);

      if (YTRecord != null) {
        YouTubeVideoViewCount = YTRecord.Value.HighestViewCount;
        YouTubeVideoId = YTRecord.Value.HighestViewedVideoId;
      }
    }


    if (vtuberRecord.Twitch != null) {
      VTuberRecord.TwitchData.Record? TwitchRecord = vtuberRecord.Twitch.GetRecord(LatestRecordTime);

      if (TwitchRecord != null) {
        TwitchVideoViewCount = TwitchRecord.Value.HighestViewCount;
        TwitchVideoId = TwitchRecord.Value.HighestViewedVideoId;
      }
    }

    if (YouTubeVideoViewCount == 0 && TwitchVideoViewCount == 0) {
      return null;
    }

    if (YouTubeVideoViewCount > TwitchVideoViewCount) {
      return new VideoInfo(type: VideoType.YouTube, id: YouTubeVideoId);
    } else {
      return new VideoInfo(type: VideoType.Twitch, id: TwitchVideoId);
    }
  }

  private static BaseCountType ToYouTubeCountType(bool hasValidRecord, ulong? subCount) {
    if (subCount.HasValue && hasValidRecord) {
      if (subCount == 0)
        return new HiddenCountType();

      return new HasCountType(_count: subCount.Value);
    }

    return new NoCountType();
  }

  private static BaseCountType ToTwitchCountType(bool hasValidRecord, ulong? followerCount) {
    if (followerCount.HasValue && hasValidRecord) {
      return new HasCountType(_count: followerCount.Value);
    }

    return new NoCountType();
  }
}
