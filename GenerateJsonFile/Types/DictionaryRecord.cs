using Common.Types;
using Common.Utils;

namespace GenerateJsonFile.Types;

// Key: VTuber ID
public class DictionaryRecord : Dictionary<string, VTuberRecord>
{
    public DictionaryRecord(TrackList trackList, List<string> excluedList, Dictionary<string, VTuberBasicData> dictBasicData)
    {
        List<string> lstId = trackList.GetIdList();

        foreach (string id in lstId)
        {
            if (excluedList.Contains(id))
            {
                continue;
            }

            bool hasBasicData = dictBasicData.TryGetValue(id, out VTuberBasicData basicData);
            string? imageUrl = hasBasicData ? basicData.GetRepresentImageUrl() : null;

            VTuberRecord vtuberRecord = new()
            {
                Id = id,
                DisplayName = trackList.GetDisplayName(id),
                DebutDate = trackList.GetDebutDate(id),
                GraduationDate = trackList.GetGraduationDate(id),
                Activity = trackList.GetActivity(id),
                GroupName = trackList.GetGroupName(id),
                Nationality = trackList.GetNationality(id),
                ImageUrl = imageUrl,

                YouTube = trackList.GetYouTubeChannelId(id) != "" ? new() { ChannelId = trackList.GetYouTubeChannelId(id) } : null,

                Twitch = trackList.GetTwitchChannelId(id) != "" ? new()
                {
                    ChannelId = trackList.GetTwitchChannelId(id),
                    ChannelName = trackList.GetTwitchChannelName(id),
                }
                : null,
            };

            this.Add(id, vtuberRecord);
        }
    }

    public void AppendStatistic(DateTime recordDateTime, Dictionary<string, VTuberStatistics> statisticsDict)
    {
        foreach (KeyValuePair<string, VTuberStatistics> vtuberStatPair in statisticsDict)
        {
            string id = vtuberStatPair.Key;
            if (!this.ContainsKey(id))
            {
                continue;
            }

            VTuberStatistics vtuberStat = vtuberStatPair.Value;

            VTuberRecord.YouTubeData.YouTubeRecord youTubeRecord = new()
            {
                SubscriberCount = vtuberStat.YouTube.SubscriberCount,
                TotalViewCount = vtuberStat.YouTube.ViewCount,
                RecentMedianViewCount = vtuberStat.YouTube.RecentMedianViewCount,
                HighestViewCount = vtuberStat.YouTube.RecentHighestViewCount,
                HighestViewedVideoId = Utility.YouTubeVideoUrlToId(vtuberStat.YouTube.HighestViewedVideoURL),
            };

            VTuberRecord.TwitchData.TwitchRecord twitchRecord = new()
            {
                FollowerCount = vtuberStat.Twitch.FollowerCount,
                RecentMedianViewCount = vtuberStat.Twitch.RecentMedianViewCount,
                HighestViewCount = vtuberStat.Twitch.RecentHighestViewCount,
                HighestViewedVideoId = Utility.TwitchVideoUrlToId(vtuberStat.Twitch.HighestViewedVideoURL),
            };

            this[id].YouTube?.AddRecord(recordDateTime, youTubeRecord);
            this[id].Twitch?.AddRecord(recordDateTime, twitchRecord);
        }
    }

    public List<VTuberRecord> GetAboutToDebutList(int beforeCurrentDay, int afterCurrentDay)
    {
        beforeCurrentDay = -beforeCurrentDay;

        DateTime currentDate = DateTime.Today;

        List<VTuberRecord> rLst = new();
        foreach (KeyValuePair<string, VTuberRecord> pair in this)
        {
            VTuberRecord record = pair.Value;

            if (record.DebutDate is null)
            {
                continue;
            }

            double days = (record.DebutDate.Value.ToDateTime(TimeOnly.MinValue) - currentDate).TotalDays;
            if (beforeCurrentDay <= days && days <= afterCurrentDay)
            {
                rLst.Add(record);
            }
        }

        return rLst;
    }

    public List<VTuberRecord> GetAboutToGraduateList()
    {
        DateTime currentDate = DateTime.Today;

        List<VTuberRecord> rLst = new();
        foreach (KeyValuePair<string, VTuberRecord> pair in this)
        {
            VTuberRecord record = pair.Value;

            if (record.GraduationDate is null)
            {
                continue;
            }

            if (Math.Abs((record.GraduationDate.Value.ToDateTime(TimeOnly.MinValue) - currentDate).TotalDays) <= 30)
            {
                rLst.Add(record);
            }
        }

        return rLst;
    }

    public VTuberRecord GetVtuberRecordByName(string name)
    {
        return this[name];
    }

    public enum GetGrowthResult
    {
        Found,
        NotExact,
        NotFound,
    }
    public GetGrowthResult GetYouTubeSubscriberCountGrowth(string id, int days, int daysLimit, out long rGrowth, out decimal rGrowthRate)
    {
        // at least one(1) day interval
        daysLimit = Math.Max(1, daysLimit);

        VTuberRecord.YouTubeData? youTubeData = this[id].YouTube;
        if (youTubeData == null)
        {
            rGrowth = 0;
            rGrowthRate = 0;
            return GetGrowthResult.NotFound;
        }

        Dictionary<DateTime, VTuberRecord.YouTubeData.YouTubeRecord>.KeyCollection lstDateTime = youTubeData.GetDateTimes();
        if (lstDateTime.Count <= 0)
        {
            rGrowth = 0;
            rGrowthRate = 0;
            return GetGrowthResult.NotFound;
        }

        DateTime latestDateTime = lstDateTime.Max();
        DateTime earlestDateTime = lstDateTime.Min();
        DateTime targetDateTime = latestDateTime - new TimeSpan(days: days, hours: 0, minutes: 0, seconds: 0);
        DateTime foundDateTime = lstDateTime.Aggregate((x, y) => (x - targetDateTime).Duration() < (y - targetDateTime).Duration() ? x : y);

        VTuberRecord.YouTubeData.YouTubeRecord? targetRecord = youTubeData.GetRecord(foundDateTime);
        ulong targetSubscriberCount = targetRecord.HasValue ? targetRecord.Value.SubscriberCount : 0;
        // previously hidden subscriber count doesn't count as growth
        if (targetSubscriberCount == 0)
        {
            rGrowth = 0;
            rGrowthRate = 0;
            return GetGrowthResult.NotFound;
        }

        VTuberRecord.YouTubeData.YouTubeRecord? currentRecord = youTubeData.GetRecord(latestDateTime);
        ulong currentSubscriberCount = currentRecord.HasValue ? currentRecord.Value.SubscriberCount : 0;

        rGrowth = (long)currentSubscriberCount - (long)targetSubscriberCount;

        if (currentSubscriberCount != 0)
            rGrowthRate = (decimal)rGrowth / currentSubscriberCount;
        else
            rGrowthRate = 0m;

        TimeSpan foundTimeDifference = (foundDateTime - targetDateTime).Duration();
        if (foundTimeDifference < new TimeSpan(days: 1, hours: 0, minutes: 0, seconds: 0))
        {
            return GetGrowthResult.Found;
        }
        else if (foundTimeDifference < new TimeSpan(days: (days - daysLimit), hours: 0, minutes: 0, seconds: 0))
        {
            return GetGrowthResult.NotExact;
        }
        else
        {
            return GetGrowthResult.NotFound;
        }
    }

    public GetGrowthResult GetYouTubeViewCountGrowth(string id, int days, int daysLimit, out decimal rGrowth, out decimal rGrowthRate)
    {
        // at least one(1) day interval
        daysLimit = Math.Max(1, daysLimit);

        VTuberRecord.YouTubeData? youTubeData = this[id].YouTube;
        if (youTubeData == null)
        {
            rGrowth = 0;
            rGrowthRate = 0;
            return GetGrowthResult.NotFound;
        }

        Dictionary<DateTime, VTuberRecord.YouTubeData.YouTubeRecord>.KeyCollection lstDateTime = youTubeData.GetDateTimes();
        if (lstDateTime.Count <= 0)
        {
            rGrowth = 0;
            rGrowthRate = 0;
            return GetGrowthResult.NotFound;
        }

        DateTime latestDateTime = lstDateTime.Max();
        DateTime earlestDateTime = lstDateTime.Min();
        DateTime targetDateTime = latestDateTime - new TimeSpan(days: days, hours: 0, minutes: 0, seconds: 0);
        DateTime foundDateTime = lstDateTime.Aggregate((x, y) => (x - targetDateTime).Duration() < (y - targetDateTime).Duration() ? x : y);

        VTuberRecord.YouTubeData.YouTubeRecord? targetRecord = youTubeData.GetRecord(foundDateTime);
        ulong targetTotalViewCount = targetRecord.HasValue ? targetRecord.Value.TotalViewCount : 0;
        if (targetTotalViewCount == 0)
        {
            rGrowth = 0;
            rGrowthRate = 0;
            return GetGrowthResult.NotFound;
        }

        VTuberRecord.YouTubeData.YouTubeRecord? currentRecord = youTubeData.GetRecord(latestDateTime);
        ulong currentTotalViewCount = currentRecord.HasValue ? currentRecord.Value.TotalViewCount : 0;

        rGrowth = (decimal)currentTotalViewCount - (decimal)targetTotalViewCount;

        if (currentTotalViewCount != 0)
            rGrowthRate = (decimal)rGrowth / currentTotalViewCount;
        else
            rGrowthRate = 0m;

        TimeSpan foundTimeDifference = (foundDateTime - targetDateTime).Duration();
        if (foundTimeDifference < new TimeSpan(days: 1, hours: 0, minutes: 0, seconds: 0))
        {
            return GetGrowthResult.Found;
        }
        else if (foundTimeDifference < new TimeSpan(days: (days - daysLimit), hours: 0, minutes: 0, seconds: 0))
        {
            return GetGrowthResult.NotExact;
        }
        else
        {
            return GetGrowthResult.NotFound;
        }
    }
}
