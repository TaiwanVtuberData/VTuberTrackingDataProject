using Common.Types;
using Common.Utils;

namespace GenerateJsonFile.Types;

// Key: VTuber ID
public class DictionaryRecord : Dictionary<string, VTuberRecord> {
  public DictionaryRecord(TrackList trackList, List<string> excluedList, Dictionary<string, VTuberBasicData> dictBasicData) {
    List<string> lstId = trackList.GetIdList();

    foreach (string id in lstId) {
      if (excluedList.Contains(id)) {
        continue;
      }

      bool hasBasicData = dictBasicData.TryGetValue(id, out VTuberBasicData basicData);
      string? imageUrl = hasBasicData ? basicData.GetRepresentImageUrl() : null;

      VTuberRecord vtuberRecord = new() {
        Id = id,
        DisplayName = trackList.GetDisplayName(id),
        DebutDate = trackList.GetDebutDate(id),
        GraduationDate = trackList.GetGraduationDate(id),
        Activity = trackList.GetActivity(id),
        GroupName = trackList.GetGroupName(id),
        Nationality = trackList.GetNationality(id),
        ImageUrl = imageUrl,

        YouTube = trackList.GetYouTubeChannelId(id) != "" ? new() {
          ChannelId = trackList.GetYouTubeChannelId(id),
          hasValidRecord = basicData.YouTube.HasValue,
        }
          : null,

        Twitch = trackList.GetTwitchChannelId(id) != "" ? new() {
          ChannelId = trackList.GetTwitchChannelId(id),
          ChannelName = trackList.GetTwitchChannelName(id),
          hasValidRecord = basicData.Twitch.HasValue,
        }
          : null,
      };

      this.Add(id, vtuberRecord);
    }
  }

  public void AppendStatistic(DateTime recordDateTime, Dictionary<string, VTuberStatistics> statisticsDict) {
    foreach (KeyValuePair<string, VTuberStatistics> vtuberStatPair in statisticsDict) {
      string id = vtuberStatPair.Key;
      if (!this.ContainsKey(id)) {
        continue;
      }

      VTuberStatistics vtuberStat = vtuberStatPair.Value;

      VTuberRecord.YouTubeData.Record youTubeRecord = new() {
        SubscriberCount = vtuberStat.YouTube.SubscriberCount,
        TotalViewCount = vtuberStat.YouTube.ViewCount,
        RecentMedianViewCount = vtuberStat.YouTube.RecentMedianViewCount,
        RecentPopularity = vtuberStat.YouTube.RecentPopularity,
        HighestViewCount = vtuberStat.YouTube.RecentHighestViewCount,
        HighestViewedVideoId = Utility.YouTubeVideoUrlToId(vtuberStat.YouTube.HighestViewedVideoURL),
      };

      VTuberRecord.TwitchData.Record twitchRecord = new() {
        FollowerCount = vtuberStat.Twitch.FollowerCount,
        RecentMedianViewCount = vtuberStat.Twitch.RecentMedianViewCount,
        RecentPopularity = vtuberStat.Twitch.RecentPopularity,
        HighestViewCount = vtuberStat.Twitch.RecentHighestViewCount,
        HighestViewedVideoId = Utility.TwitchVideoUrlToId(vtuberStat.Twitch.HighestViewedVideoURL),
      };

      this[id].YouTube?.AddRecord(recordDateTime, youTubeRecord);
      this[id].Twitch?.AddRecord(recordDateTime, twitchRecord);
    }
  }
  public void AppendBasicData(DateTime recordDateTime, Dictionary<string, VTuberStatistics> statisticsDict) {
    foreach (KeyValuePair<string, VTuberStatistics> vtuberStatPair in statisticsDict) {
      string id = vtuberStatPair.Key;
      if (!this.ContainsKey(id)) {
        continue;
      }

      VTuberStatistics vtuberData = vtuberStatPair.Value;

      if (this[id].YouTube != null) {
        VTuberRecord.YouTubeData.BasicData YTData = new() {
          SubscriberCount = vtuberData.YouTube.SubscriberCount,
          TotalViewCount = vtuberData.YouTube.ViewCount,
        };

        this[id].YouTube?.AddBasicData(recordDateTime, YTData);
      }

      if (this[id].Twitch != null) {
        VTuberRecord.TwitchData.BasicData twitchData = new() {
          FollowerCount = vtuberData.Twitch.FollowerCount,
        };

        this[id].Twitch?.AddBasicData(recordDateTime, twitchData);
      }
    }
  }
  public void AppendBasicData(DateTime recordDateTime, Dictionary<string, VTuberBasicData> dictBasicData) {
    foreach (KeyValuePair<string, VTuberBasicData> vtuberDataPair in dictBasicData) {
      string id = vtuberDataPair.Key;
      if (!this.ContainsKey(id)) {
        continue;
      }

      VTuberBasicData vtuberData = vtuberDataPair.Value;

      if (vtuberData.YouTube.HasValue) {
        VTuberRecord.YouTubeData.BasicData YTData = new() {
          SubscriberCount = vtuberData.YouTube.Value.SubscriberCount ?? 0,
          TotalViewCount = vtuberData.YouTube.Value.ViewCount ?? 0,
        };

        this[id].YouTube?.AddBasicData(recordDateTime, YTData);
      }

      if (vtuberData.Twitch.HasValue) {
        VTuberRecord.TwitchData.BasicData twitchData = new() {
          FollowerCount = vtuberData.Twitch.Value.FollowerCount,
        };

        this[id].Twitch?.AddBasicData(recordDateTime, twitchData);
      }
    }
  }

  public VTuberRecord GetRecordByYouTubeId(string YouTubeId) {
    return this.Where(e => YouTubeId == e.Value.YouTube?.ChannelId).Select(e => e.Value).ToList()[0];
  }

  public List<VTuberRecord> GetAboutToDebutList(DateOnly date) {
    List<VTuberRecord> rLst = new();
    foreach (KeyValuePair<string, VTuberRecord> pair in this) {
      VTuberRecord record = pair.Value;

      if (record.DebutDate is null) {
        continue;
      }

      double days = (record.DebutDate.Value.ToDateTime(TimeOnly.MinValue) - date.ToDateTime(TimeOnly.MinValue)).TotalDays;
      if (days == 0.0) {
        rLst.Add(record);
      }
    }

    return rLst;
  }

  public enum GrowthType {
    Found,
    NotExact,
    NotFound,
  }

  public class GrowthResult {
    public GrowthType GrowthType { get; init; } = GrowthType.NotFound;
    public decimal Growth { get; init; } = 0;
    public decimal GrowthRate { get; init; } = 0;
  }


  public GrowthResult GetYouTubeSubscriberCountGrowth(string id, int days, int daysLimit) {
    // at least one(1) day interval
    daysLimit = Math.Max(1, daysLimit);

    VTuberRecord.YouTubeData? youTubeData = this[id].YouTube;
    if (youTubeData == null)
      return new GrowthResult();

    Dictionary<DateTime, VTuberRecord.YouTubeData.BasicData>.KeyCollection lstDateTime = youTubeData.GetBasicDataDateTimes();
    if (lstDateTime.Count <= 0)
      return new GrowthResult();

    DateTime latestDateTime = lstDateTime.Max();
    DateTime earlestDateTime = lstDateTime.Min();
    DateTime targetDateTime = latestDateTime - new TimeSpan(days: days, hours: 0, minutes: 0, seconds: 0);
    DateTime foundDateTime = lstDateTime.Aggregate((x, y) => (x - targetDateTime).Duration() < (y - targetDateTime).Duration() ? x : y);

    VTuberRecord.YouTubeData.BasicData? targetBasicData = youTubeData.GetBasicData(foundDateTime);
    ulong targetSubscriberCount = targetBasicData.HasValue ? targetBasicData.Value.SubscriberCount : 0;
    // previously hidden subscriber count doesn't count as growth
    if (targetSubscriberCount == 0)
      return new GrowthResult();

    VTuberRecord.YouTubeData.BasicData? currentBasicData = youTubeData.GetBasicData(latestDateTime);
    ulong currentSubscriberCount = currentBasicData.HasValue ? currentBasicData.Value.SubscriberCount : 0;

    // return result
    long rGrowth = (long)currentSubscriberCount - (long)targetSubscriberCount;

    decimal rGrowthRate;
    if (currentSubscriberCount != 0)
      rGrowthRate = (decimal)rGrowth / currentSubscriberCount;
    else
      rGrowthRate = 0m;

    TimeSpan foundTimeDifference = (foundDateTime - targetDateTime).Duration();
    if (foundTimeDifference < new TimeSpan(days: 1, hours: 0, minutes: 0, seconds: 0)) {
      return new GrowthResult { GrowthType = GrowthType.Found, Growth = rGrowth, GrowthRate = rGrowthRate };
    } else if (foundTimeDifference < new TimeSpan(days: (days - daysLimit), hours: 0, minutes: 0, seconds: 0)) {
      return new GrowthResult { GrowthType = GrowthType.NotExact, Growth = rGrowth, GrowthRate = rGrowthRate };
    } else {
      return new GrowthResult();
    }
  }

  public GrowthResult GetYouTubeViewCountGrowth(string id, int days, int daysLimit) {
    // at least one(1) day interval
    daysLimit = Math.Max(1, daysLimit);

    VTuberRecord.YouTubeData? youTubeData = this[id].YouTube;
    if (youTubeData == null)
      return new GrowthResult();

    Dictionary<DateTime, VTuberRecord.YouTubeData.BasicData>.KeyCollection lstDateTime = youTubeData.GetBasicDataDateTimes();
    if (lstDateTime.Count <= 0)
      return new GrowthResult();

    DateTime latestDateTime = lstDateTime.Max();
    DateTime earlestDateTime = lstDateTime.Min();
    DateTime targetDateTime = latestDateTime - new TimeSpan(days: days, hours: 0, minutes: 0, seconds: 0);
    DateTime foundDateTime = lstDateTime.Aggregate((x, y) => (x - targetDateTime).Duration() < (y - targetDateTime).Duration() ? x : y);

    VTuberRecord.YouTubeData.BasicData? targetBasicData = youTubeData.GetBasicData(foundDateTime);
    decimal targetTotalViewCount = targetBasicData.HasValue ? targetBasicData.Value.TotalViewCount : 0;
    if (targetTotalViewCount == 0)
      return new GrowthResult();

    VTuberRecord.YouTubeData.BasicData? currentBasicData = youTubeData.GetBasicData(latestDateTime);
    decimal currentTotalViewCount = currentBasicData.HasValue ? currentBasicData.Value.TotalViewCount : 0;

    // return result
    decimal rGrowth = currentTotalViewCount - targetTotalViewCount;

    decimal rGrowthRate;
    if (currentTotalViewCount != 0)
      rGrowthRate = rGrowth / currentTotalViewCount;
    else
      rGrowthRate = 0m;

    TimeSpan foundTimeDifference = (foundDateTime - targetDateTime).Duration();
    if (foundTimeDifference < new TimeSpan(days: 1, hours: 0, minutes: 0, seconds: 0)) {
      return new GrowthResult { GrowthType = GrowthType.Found, Growth = rGrowth, GrowthRate = rGrowthRate };
    } else if (foundTimeDifference < new TimeSpan(days: (days - daysLimit), hours: 0, minutes: 0, seconds: 0)) {
      return new GrowthResult { GrowthType = GrowthType.NotExact, Growth = rGrowth, GrowthRate = rGrowthRate };
    } else {
      return new GrowthResult();
    }
  }
}
