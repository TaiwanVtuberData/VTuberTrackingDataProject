using Common.Types;
using Common.Utils;

namespace GenerateJsonFile.Types;

public class DictionaryRecord : Dictionary<string, VTuberRecord>
{
    public DictionaryRecord(TrackList trackList, List<string> excluedList, Dictionary<string, VTuberBasicData> dictBasicData)
    {
        List<string> lstDisplayName = trackList.GetDisplayNameList();

        foreach (string displayName in lstDisplayName)
        {
            if (excluedList.Contains(displayName))
            {
                continue;
            }

            bool hasBasicData = dictBasicData.TryGetValue(displayName, out VTuberBasicData? basicData);
            string imageUrl = (hasBasicData && basicData is not null) ? basicData.GetRepresentImageUrl() : "data:image/gif;base64,R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7";

            VTuberRecord vtuberRecord = new()
            {
                DisplayName = displayName,
                Id = trackList.GetId(displayName),
                DebutDate = trackList.GetDebutDate(displayName),
                GraduationDate = trackList.GetGraduationDate(displayName),
                IsActive = trackList.GetIsActive(displayName),
                GroupName = trackList.GetGroupNameByName(displayName),
                Nationality = trackList.GetNationalityByName(displayName),
                ThumbnailUrl = imageUrl,

                YouTube = new()
                {
                    ChannelId = trackList.GetYouTubeChannelIdByName(displayName),
                },

                Twitch = new()
                {
                    ChannelId = trackList.GetTwitchChannelIdByName(displayName),
                    ChannelName = trackList.GetTwitchChannelNameByName(displayName),
                },
            };

            this.Add(displayName, vtuberRecord);
        }
    }

    public void AppendStatistic(DateTime recordDateTime, Dictionary<string, VTuberStatistics> statisticsDict)
    {
        foreach (KeyValuePair<string, VTuberStatistics> vtuberStatPair in statisticsDict)
        {
            string displayName = vtuberStatPair.Key;
            if (!this.ContainsKey(displayName))
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

            this[displayName].YouTube.DictRecord.Add(recordDateTime, youTubeRecord);
            this[displayName].Twitch.DictRecord.Add(recordDateTime, twitchRecord);
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

            double days = (record.DebutDate - currentDate).TotalDays;
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

            if (Math.Abs((record.GraduationDate - currentDate).TotalDays) <= 30)
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
    public GetGrowthResult GetYouTubeSubscriberCountGrowth(string displayName, int days, int daysLimit, out long rGrowth, out decimal rGrowthRate)
    {
        // at least one(1) day interval
        daysLimit = Math.Max(1, daysLimit);

        Dictionary<DateTime, VTuberRecord.YouTubeData.YouTubeRecord>.KeyCollection lstDateTime = this[displayName].YouTube.DictRecord.Keys;
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

        // previously hidden subscriber count doesn't count as growth
        if (this[displayName].YouTube.DictRecord[foundDateTime].SubscriberCount == 0)
        {
            rGrowth = 0;
            rGrowthRate = 0;
            return GetGrowthResult.NotFound;
        }

        rGrowth = (long)this[displayName].YouTube.DictRecord[latestDateTime].SubscriberCount
            - (long)this[displayName].YouTube.DictRecord[foundDateTime].SubscriberCount;

        if (this[displayName].YouTube.DictRecord[latestDateTime].SubscriberCount != 0)
            rGrowthRate = (decimal)rGrowth / (decimal)this[displayName].YouTube.DictRecord[latestDateTime].SubscriberCount;
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

    public GetGrowthResult GetYouTubeViewCountGrowth(string displayName, int days, int daysLimit, out decimal rGrowth, out decimal rGrowthRate)
    {
        // at least one(1) day interval
        daysLimit = Math.Max(1, daysLimit);

        Dictionary<DateTime, VTuberRecord.YouTubeData.YouTubeRecord>.KeyCollection lstDateTime = this[displayName].YouTube.DictRecord.Keys;
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

        // previously hidden subscriber count doesn't count as growth
        if (this[displayName].YouTube.DictRecord[foundDateTime].TotalViewCount == 0)
        {
            rGrowth = 0;
            rGrowthRate = 0;
            return GetGrowthResult.NotFound;
        }

        rGrowth = (decimal)this[displayName].YouTube.DictRecord[latestDateTime].TotalViewCount
            - (decimal)this[displayName].YouTube.DictRecord[foundDateTime].TotalViewCount;

        if (this[displayName].YouTube.DictRecord[latestDateTime].TotalViewCount != 0)
            rGrowthRate = (decimal)rGrowth / (decimal)this[displayName].YouTube.DictRecord[latestDateTime].TotalViewCount;
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
