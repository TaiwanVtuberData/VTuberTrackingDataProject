using Common.Types;
using Common.Types.Basic;
using Common.Utils;

namespace GenerateRecordList.Types;

// Key: VTuber ID
public class DictionaryRecord : Dictionary<VTuberId, VTuberRecord>
{
    public DictionaryRecord(
        TrackList trackList,
        List<VTuberId> excluedList,
        Dictionary<VTuberId, VTuberBasicData> dictBasicData
    )
    {
        List<VTuberId> lstId = trackList.GetIdList();

        foreach (VTuberId id in lstId)
        {
            if (excluedList.Contains(id))
            {
                continue;
            }

            bool hasBasicData = dictBasicData.TryGetValue(id, out VTuberBasicData basicData);
            string? imageUrl = hasBasicData ? basicData.GetRepresentImageUrl() : null;

            VTuberRecord vtuberRecord =
                new()
                {
                    Id = id,
                    DisplayName = trackList.GetDisplayName(id),
                    DebutDate = trackList.GetDebutDate(id),
                    GraduationDate = trackList.GetGraduationDate(id),
                    Activity = trackList.GetActivity(id),
                    GroupName = trackList.GetGroupName(id)?.Value,
                    Nationality = trackList.GetNationality(id),
                    ImageUrl = imageUrl,

                    YouTube =
                        trackList.GetYouTubeChannelId(id) != ""
                            ? new()
                            {
                                ChannelId = trackList.GetYouTubeChannelId(id),
                                hasValidRecord = basicData.YouTube.HasValue,
                            }
                            : null,

                    Twitch =
                        trackList.GetTwitchChannelId(id) != ""
                            ? new()
                            {
                                ChannelId = trackList.GetTwitchChannelId(id),
                                ChannelName = trackList.GetTwitchChannelName(id),
                                hasValidRecord = basicData.Twitch.HasValue,
                            }
                            : null,
                };

            this.Add(id, vtuberRecord);
        }
    }

    public void AppendStatistic(
        DateTimeOffset recordDateTime,
        Dictionary<VTuberId, VTuberStatistics> statisticsDict
    )
    {
        foreach (KeyValuePair<VTuberId, VTuberStatistics> vtuberStatPair in statisticsDict)
        {
            VTuberId id = vtuberStatPair.Key;
            if (!this.ContainsKey(id))
            {
                continue;
            }

            VTuberStatistics vtuberStat = vtuberStatPair.Value;

            VTuberRecord.YouTubeData.Record youTubeRecord =
                new(
                    SubscriberCount: vtuberStat.YouTube.Basic.SubscriberCount,
                    TotalViewCount: vtuberStat.YouTube.Basic.ViewCount,
                    RecentTotalMedianViewCount: vtuberStat.YouTube.Recent.Total.MedialViewCount,
                    RecentLivestreamMedianViewCount: vtuberStat
                        .YouTube
                        .Recent
                        .Livestream
                        .MedialViewCount,
                    RecentVideoMedianViewCount: vtuberStat.YouTube.Recent.Video.MedialViewCount,
                    RecentPopularity: vtuberStat.YouTube.Recent.Total.Popularity,
                    HighestViewCount: vtuberStat.YouTube.Recent.Total.HighestViewCount,
                    HighestViewedVideoId: Utility.YouTubeVideoUrlToId(
                        vtuberStat.YouTube.Recent.Total.HighestViewdUrl
                    )
                );

            VTuberRecord.TwitchData.Record twitchRecord =
                new(
                    FollowerCount: vtuberStat.Twitch.FollowerCount,
                    RecentMedianViewCount: vtuberStat.Twitch.RecentMedianViewCount,
                    RecentPopularity: vtuberStat.Twitch.RecentPopularity,
                    HighestViewCount: vtuberStat.Twitch.RecentHighestViewCount,
                    HighestViewedVideoId: Utility.TwitchVideoUrlToId(
                        vtuberStat.Twitch.HighestViewedVideoURL
                    )
                );

            this[id].YouTube?.AddRecord(recordDateTime, youTubeRecord);
            this[id].Twitch?.AddRecord(recordDateTime, twitchRecord);
        }
    }

    public void AppendBasicData(
        DateTimeOffset recordDateTime,
        Dictionary<VTuberId, VTuberStatistics> statisticsDict
    )
    {
        foreach (KeyValuePair<VTuberId, VTuberStatistics> vtuberStatPair in statisticsDict)
        {
            VTuberId id = vtuberStatPair.Key;
            if (!this.ContainsKey(id))
            {
                continue;
            }

            VTuberStatistics vtuberData = vtuberStatPair.Value;

            if (this[id].YouTube != null)
            {
                VTuberRecord.YouTubeData.BasicData YTData =
                    new(
                        SubscriberCount: vtuberData.YouTube.Basic.SubscriberCount,
                        TotalViewCount: vtuberData.YouTube.Basic.ViewCount
                    );

                this[id].YouTube?.AddBasicData(recordDateTime, YTData);
            }

            if (this[id].Twitch != null)
            {
                VTuberRecord.TwitchData.BasicData twitchData =
                    new(FollowerCount: vtuberData.Twitch.FollowerCount);

                this[id].Twitch?.AddBasicData(recordDateTime, twitchData);
            }
        }
    }

    public void AppendBasicData(
        DateTimeOffset recordDateTime,
        Dictionary<VTuberId, VTuberBasicData> dictBasicData
    )
    {
        foreach (KeyValuePair<VTuberId, VTuberBasicData> vtuberDataPair in dictBasicData)
        {
            VTuberId id = vtuberDataPair.Key;
            if (!this.ContainsKey(id))
            {
                continue;
            }

            VTuberBasicData vtuberData = vtuberDataPair.Value;

            if (vtuberData.YouTube.HasValue)
            {
                VTuberRecord.YouTubeData.BasicData YTData =
                    new(
                        SubscriberCount: vtuberData.YouTube.Value.SubscriberCount ?? 0,
                        TotalViewCount: vtuberData.YouTube.Value.ViewCount ?? 0
                    );

                this[id].YouTube?.AddBasicData(recordDateTime, YTData);
            }

            if (vtuberData.Twitch.HasValue)
            {
                VTuberRecord.TwitchData.BasicData twitchData =
                    new(FollowerCount: vtuberData.Twitch.Value.FollowerCount);

                this[id].Twitch?.AddBasicData(recordDateTime, twitchData);
            }
        }
    }

    public VTuberRecord GetRecordByYouTubeId(string YouTubeId)
    {
        return this.Where(e => YouTubeId == e.Value.YouTube?.ChannelId)
            .Select(e => e.Value)
            .ToList()[0];
    }

    public List<VTuberRecord> GetAboutToDebutList(DateOnly date)
    {
        List<VTuberRecord> rLst = [];
        foreach (KeyValuePair<VTuberId, VTuberRecord> pair in this)
        {
            VTuberRecord record = pair.Value;

            if (record.DebutDate is null)
            {
                continue;
            }

            double days = (
                record.DebutDate.Value.ToDateTime(TimeOnly.MinValue)
                - date.ToDateTime(TimeOnly.MinValue)
            ).TotalDays;
            if (days == 0.0)
            {
                rLst.Add(record);
            }
        }

        return rLst;
    }

    public enum GrowthType
    {
        Found,
        NotExact,
        NotFound,
    }

    public class GrowthResult
    {
        public GrowthType GrowthType { get; init; } = GrowthType.NotFound;
        public decimal Growth { get; init; } = 0;
        public decimal GrowthRate { get; init; } = 0;
    }

    public GrowthResult GetYouTubeSubscriberCountGrowth(VTuberId id, int days, int daysLimit)
    {
        // at least one(1) day interval
        daysLimit = Math.Max(1, daysLimit);

        VTuberRecord.YouTubeData? youTubeData = this[id].YouTube;
        if (youTubeData == null)
            return new GrowthResult();

        Dictionary<DateTimeOffset, VTuberRecord.YouTubeData.BasicData>.KeyCollection lstDateTime =
            youTubeData.GetBasicDataDateTimes();
        if (lstDateTime.Count <= 0)
            return new GrowthResult();

        DateTimeOffset latestDateTime = lstDateTime.Max();
        DateTimeOffset earliestDateTime = lstDateTime.Min();
        DateTimeOffset targetDateTime = latestDateTime - TimeSpan.FromDays(days);
        DateTimeOffset foundDateTime = lstDateTime.Aggregate(
            (x, y) => (x - targetDateTime).Duration() < (y - targetDateTime).Duration() ? x : y
        );

        VTuberRecord.YouTubeData.BasicData? targetBasicData = youTubeData.GetBasicData(
            foundDateTime
        );
        ulong targetSubscriberCount = targetBasicData?.SubscriberCount ?? 0;
        // previously hidden subscriber count doesn't count as growth
        if (targetSubscriberCount == 0)
            return new GrowthResult();

        VTuberRecord.YouTubeData.BasicData? currentBasicData = youTubeData.GetBasicData(
            latestDateTime
        );
        ulong currentSubscriberCount = currentBasicData?.SubscriberCount ?? 0;

        // return result
        long rGrowth = (long)currentSubscriberCount - (long)targetSubscriberCount;

        decimal rGrowthRate;
        if (currentSubscriberCount != 0)
            rGrowthRate = (decimal)rGrowth / currentSubscriberCount;
        else
            rGrowthRate = 0m;

        TimeSpan foundTimeDifference = (foundDateTime - targetDateTime).Duration();
        if (foundTimeDifference < TimeSpan.FromDays(1))
        {
            return new GrowthResult
            {
                GrowthType = GrowthType.Found,
                Growth = rGrowth,
                GrowthRate = rGrowthRate,
            };
        }
        else if (
            foundTimeDifference
            < new TimeSpan(days: (days - daysLimit), hours: 0, minutes: 0, seconds: 0)
        )
        {
            return new GrowthResult
            {
                GrowthType = GrowthType.NotExact,
                Growth = rGrowth,
                GrowthRate = rGrowthRate,
            };
        }
        else
        {
            return new GrowthResult();
        }
    }

    public GrowthResult GetYouTubeViewCountGrowth(VTuberId id, int days, int daysLimit)
    {
        // at least one(1) day interval
        daysLimit = Math.Max(1, daysLimit);

        VTuberRecord.YouTubeData? youTubeData = this[id].YouTube;
        if (youTubeData == null)
            return new GrowthResult();

        Dictionary<DateTimeOffset, VTuberRecord.YouTubeData.BasicData>.KeyCollection lstDateTime =
            youTubeData.GetBasicDataDateTimes();
        if (lstDateTime.Count <= 0)
            return new GrowthResult();

        DateTimeOffset latestDateTime = lstDateTime.Max();
        DateTimeOffset earlestDateTime = lstDateTime.Min();
        DateTimeOffset targetDateTime = latestDateTime - TimeSpan.FromDays(days);
        DateTimeOffset foundDateTime = lstDateTime.Aggregate(
            (x, y) => (x - targetDateTime).Duration() < (y - targetDateTime).Duration() ? x : y
        );

        VTuberRecord.YouTubeData.BasicData? targetBasicData = youTubeData.GetBasicData(
            foundDateTime
        );
        decimal targetTotalViewCount = targetBasicData?.TotalViewCount ?? 0;
        if (targetTotalViewCount == 0)
            return new GrowthResult();

        VTuberRecord.YouTubeData.BasicData? currentBasicData = youTubeData.GetBasicData(
            latestDateTime
        );
        decimal currentTotalViewCount = currentBasicData?.TotalViewCount ?? 0;

        // return result
        decimal rGrowth = currentTotalViewCount - targetTotalViewCount;

        decimal rGrowthRate;
        if (currentTotalViewCount != 0)
            rGrowthRate = rGrowth / currentTotalViewCount;
        else
            rGrowthRate = 0m;

        TimeSpan foundTimeDifference = (foundDateTime - targetDateTime).Duration();
        if (foundTimeDifference < TimeSpan.FromDays(1))
        {
            return new GrowthResult
            {
                GrowthType = GrowthType.Found,
                Growth = rGrowth,
                GrowthRate = rGrowthRate,
            };
        }
        else if (foundTimeDifference < TimeSpan.FromDays(days - daysLimit))
        {
            return new GrowthResult
            {
                GrowthType = GrowthType.NotExact,
                Growth = rGrowth,
                GrowthRate = rGrowthRate,
            };
        }
        else
        {
            return new GrowthResult();
        }
    }
}
