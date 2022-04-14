using Common.Types;
using GenerateJsonFile.Types;
using GenerateJsonFile.Utils;

namespace GenerateJsonFile;

class DictionaryRecordToJsonStruct
{
    private readonly DateTime TodayDate;
    private readonly string NationalityFilter;

    public DictionaryRecordToJsonStruct(DateTime todayDate, string nationalityFilter)
    {
        TodayDate = todayDate;
        NationalityFilter = nationalityFilter;
    }

    private static VideoInfo? GetPopularVideo(VTuberRecord vtuberRecord, DateTime latestRecordTime)
    {
        ulong YouTubeVideoViewCount = vtuberRecord.YouTube.GetLatestHighestViewCount(latestRecordTime);
        ulong TwitchVideoViewCount = vtuberRecord.Twitch.GetLatestHighestViewCount(latestRecordTime);

        if (YouTubeVideoViewCount == 0 && TwitchVideoViewCount == 0)
        {
            return null;
        }

        if (YouTubeVideoViewCount > TwitchVideoViewCount)
        {
            return new VideoInfo() { type = VideoType.YouTube, id = vtuberRecord.YouTube.DictRecord[latestRecordTime].HighestViewedVideoId };
        }
        else
        {
            return new VideoInfo() { type = VideoType.Twitch, id = vtuberRecord.Twitch.DictRecord[latestRecordTime].HighestViewedVideoId };
        }
    }

    public List<Types.VTuberData> All(DictionaryRecord dictRecord, DateTime latestRecordTime, int? count)
    {
        List<Types.VTuberData> rLst = new();

        foreach (KeyValuePair<string, VTuberRecord> vtuberStatPair in dictRecord
            .Where(p => p.Value.Nationality.Contains(NationalityFilter))
            .OrderByDescending(p => p, new VTuberRecordComparator.CombinedCount(latestRecordTime))
            .Take(count ?? int.MaxValue))
        {
            string displayName = vtuberStatPair.Key;
            VTuberRecord record = vtuberStatPair.Value;

            ulong sub = record.YouTube.GetLatestSubscriberCount(latestRecordTime);
            bool hasDebut = TodayDate > record.DebutDate;

            Types.VTuberData vTuberData = new()
            {
                id = record.Id,
                activity = record.IsActive ? hasDebut ? Activity.active : Activity.preparing : Activity.graduate,
                name = record.DisplayName,
                imgUrl = record.ThumbnailUrl,
                YouTube = record.YouTube.ChannelId == "" ? null : new Types.YouTubeData() { id = record.YouTube.ChannelId, subscriberCount = sub == 0 ? null : sub },
                Twitch = record.Twitch.ChannelName == "" ? null : new Types.TwitchData() { id = record.Twitch.ChannelName, followerCount = record.Twitch.GetLatestFollowerCount(latestRecordTime) },
                popularVideo = GetPopularVideo(record, latestRecordTime),
                group = record.GroupName == "" ? null : record.GroupName,
                nationality = record.Nationality,
            };

            rLst.Add(vTuberData);
        }

        return rLst;
    }

    private static string GetGrowthResultToString(DictionaryRecord.GetGrowthResult getGrowthResult)
    {
        return getGrowthResult switch
        {
            DictionaryRecord.GetGrowthResult.Found => "full",
            DictionaryRecord.GetGrowthResult.NotExact => "partial",
            _ => "none",
        };
    }

    public List<VTuberGrowthData> GrowingVTubers(DictionaryRecord dictRecord, DateTime latestRecordTime, int? count)
    {
        Dictionary<string, YouTubeGrowthData> dictGrowth = new(dictRecord.Count);

        foreach (KeyValuePair<string, VTuberRecord> vtuberStatPair in dictRecord)
        {
            string displayName = vtuberStatPair.Key;
            VTuberRecord record = vtuberStatPair.Value;

            DictionaryRecord.GetGrowthResult _7DaysResult = dictRecord.GetYouTubeSubscriberCountGrowth(displayName, days: 7, daysLimit: 1, out long _7DaysGrowth, out decimal _7DaysGrowthRate);
            DictionaryRecord.GetGrowthResult _30DaysResult = dictRecord.GetYouTubeSubscriberCountGrowth(displayName, days: 30, daysLimit: 7, out long _30DaysGrowth, out decimal _30DaysGrowthRate);

            YouTubeGrowthData growthData = new()
            {
                _7DaysGrowth = new GrowthData() { diff = _7DaysGrowth, recordType = GetGrowthResultToString(_7DaysResult) },
                _30DaysGrowth = new GrowthData() { diff = _30DaysGrowth, recordType = GetGrowthResultToString(_30DaysResult) },
                subscriberCount = record.YouTube.GetLatestSubscriberCount(latestRecordTime),
                Nationality = record.Nationality,
            };

            dictGrowth.Add(displayName, growthData);
        }

        List<VTuberGrowthData> rLst = new();

        foreach (KeyValuePair<string, YouTubeGrowthData> growthPair in dictGrowth
            .Where(p => p.Value.Nationality.Contains(NationalityFilter))
            .Where(p => (p.Value.subscriberCount != null) && (p.Value.subscriberCount != 0))
            .Where(p => p.Value._7DaysGrowth.diff >= 100)
            .Where(p => dictRecord[p.Key].YouTube.DictRecord.ContainsKey(latestRecordTime))
            .OrderByDescending(p => (p.Value._7DaysGrowth.diff) / (decimal)p.Value.subscriberCount)
            .Take(count ?? int.MaxValue))
        {
            string displayName = growthPair.Key;
            YouTubeGrowthData youTubeGrowthData = growthPair.Value;

            VTuberRecord record = dictRecord[displayName];

            ulong sub = record.YouTube.GetLatestSubscriberCount(latestRecordTime);
            bool hasDebut = TodayDate > record.DebutDate;

            VTuberGrowthData vTuberData = new()
            {
                id = record.Id,
                activity = record.IsActive ? hasDebut ? Activity.active : Activity.preparing : Activity.graduate,
                name = record.DisplayName,
                imgUrl = record.ThumbnailUrl,
                YouTube = record.YouTube.ChannelId == "" ? null : new YouTubeGrowthData()
                {
                    id = record.YouTube.ChannelId,
                    subscriberCount = sub == 0 ? null : sub,
                    _7DaysGrowth = youTubeGrowthData._7DaysGrowth,
                    _30DaysGrowth = youTubeGrowthData._30DaysGrowth,
                },
                Twitch = record.Twitch.ChannelName == "" ? null : new Types.TwitchData() { id = record.Twitch.ChannelName, followerCount = record.Twitch.GetLatestFollowerCount(latestRecordTime) },
                popularVideo = GetPopularVideo(record, latestRecordTime),
                group = record.GroupName == "" ? null : record.GroupName,
                nationality = record.Nationality,
            };

            rLst.Add(vTuberData);
        }

        return rLst;
    }

    private class _7DaysGrowthComparer : IComparer<YouTubeGrowthData>
    {
        public int Compare(YouTubeGrowthData? a, YouTubeGrowthData? b)
        {
            if (a == null || b == null)
                return 0;

            return a._7DaysGrowth.diff.CompareTo(b._7DaysGrowth.diff);
        }
    }

    private class _30DaysGrowthComparer : IComparer<YouTubeGrowthData>
    {
        public int Compare(YouTubeGrowthData? a, YouTubeGrowthData? b)
        {
            if (a == null || b == null)
                return 0;

            return a._30DaysGrowth.diff.CompareTo(b._30DaysGrowth.diff);
        }
    }

    private static IComparer<YouTubeGrowthData> GetSortFunction(SortBy sortBy)
    {
        return sortBy switch
        {
            SortBy._30Days => new _30DaysGrowthComparer(),
            _ => new _7DaysGrowthComparer(),
        };
    }

    public List<VTuberGrowthData> VTubersViewCountChange(DictionaryRecord dictRecord, DateTime latestRecordTime, SortBy sortBy, int? count)
    {
        Dictionary<string, YouTubeGrowthData> dictGrowth = new(dictRecord.Count);

        foreach (KeyValuePair<string, VTuberRecord> vtuberStatPair in dictRecord)
        {
            string displayName = vtuberStatPair.Key;
            VTuberRecord record = vtuberStatPair.Value;

            DictionaryRecord.GetGrowthResult _7DaysResult = dictRecord.GetYouTubeViewCountGrowth(displayName, days: 7, daysLimit: 1, out decimal _7DaysGrowth, out decimal _7DaysGrowthRate);
            DictionaryRecord.GetGrowthResult _30DaysResult = dictRecord.GetYouTubeViewCountGrowth(displayName, days: 30, daysLimit: 7, out decimal _30DaysGrowth, out decimal _30DaysGrowthRate);

            YouTubeGrowthData growthData = new()
            {
                _7DaysGrowth = new GrowthData() { diff = _7DaysGrowth, recordType = GetGrowthResultToString(_7DaysResult) },
                _30DaysGrowth = new GrowthData() { diff = _30DaysGrowth, recordType = GetGrowthResultToString(_30DaysResult) },
                totalViewCount = record.YouTube.GetLatestTotalViewCount(latestRecordTime),
                Nationality = record.Nationality,
            };

            dictGrowth.Add(displayName, growthData);
        }

        List<VTuberGrowthData> rLst = new();

        foreach (KeyValuePair<string, YouTubeGrowthData> growthPair in dictGrowth
            .Where(p => p.Value.Nationality.Contains(NationalityFilter))
            .Where(p => (p.Value.totalViewCount != null) && (p.Value.totalViewCount != 0))
            .Where(p => p.Value._7DaysGrowth.diff >= 0)
            .Where(p => dictRecord[p.Key].YouTube.DictRecord.ContainsKey(latestRecordTime))
            .OrderByDescending(p => p.Value, GetSortFunction(sortBy))
            .Take(count ?? int.MaxValue))
        {
            string displayName = growthPair.Key;
            YouTubeGrowthData youTubeGrowthData = growthPair.Value;

            VTuberRecord record = dictRecord[displayName];

            ulong sub = record.YouTube.GetLatestTotalViewCount(latestRecordTime);
            bool hasDebut = TodayDate > record.DebutDate;

            VTuberGrowthData vTuberData = new()
            {
                id = record.Id,
                activity = record.IsActive ? hasDebut ? Activity.active : Activity.preparing : Activity.graduate,
                name = record.DisplayName,
                imgUrl = record.ThumbnailUrl,
                YouTube = record.YouTube.ChannelId == "" ? null : new YouTubeGrowthData()
                {
                    id = record.YouTube.ChannelId,
                    totalViewCount = sub == 0 ? null : sub,
                    _7DaysGrowth = youTubeGrowthData._7DaysGrowth,
                    _30DaysGrowth = youTubeGrowthData._30DaysGrowth,
                },
                Twitch = record.Twitch.ChannelName == "" ? null : new Types.TwitchData() { id = record.Twitch.ChannelName, followerCount = record.Twitch.GetLatestFollowerCount(latestRecordTime) },
                popularVideo = GetPopularVideo(record, latestRecordTime),
                group = record.GroupName == "" ? null : record.GroupName,
                nationality = record.Nationality,
            };

            rLst.Add(vTuberData);
        }

        return rLst;
    }

    public List<VTuberDebutData> DebutVTubers(DictionaryRecord dictRecord, DateTime latestRecordTime, uint daysBefore, uint daysAfter)
    {
        List<VTuberDebutData> rLst = new();

        DateTime _30DaysBefore = TodayDate.AddDays(-daysBefore);
        DateTime _30DaysAfter = TodayDate.AddDays(daysAfter);

        foreach (KeyValuePair<string, VTuberRecord> vtuberStatPair in dictRecord
            .Where(p => p.Value.Nationality.Contains(NationalityFilter))
            .Where(p => _30DaysBefore <= p.Value.DebutDate && p.Value.DebutDate < _30DaysAfter)
            .OrderByDescending(p => p.Value.DebutDate))
        {
            string displayName = vtuberStatPair.Key;
            VTuberRecord record = vtuberStatPair.Value;

            ulong sub = record.YouTube.GetLatestSubscriberCount(latestRecordTime);
            bool hasDebut = TodayDate > record.DebutDate;

            VTuberDebutData vTuberData = new(
                id: record.Id,
                activity: record.IsActive ? hasDebut ? Activity.active : Activity.preparing : Activity.graduate,
                name: record.DisplayName,
                imgUrl: record.ThumbnailUrl,
                YouTube: record.YouTube.ChannelId == "" ? null : new Types.YouTubeData() { id = record.YouTube.ChannelId, subscriberCount = sub == 0 ? null : sub },
                Twitch: record.Twitch.ChannelName == "" ? null : new Types.TwitchData() { id = record.Twitch.ChannelName, followerCount = record.Twitch.GetLatestFollowerCount(latestRecordTime) },
                popularVideo: GetPopularVideo(record, latestRecordTime),
                group: record.GroupName == "" ? null : record.GroupName,
                nationality: record.Nationality,
                debutDate: record.DebutDate.ToString("yyyy-MM-dd"));

            rLst.Add(vTuberData);
        }

        return rLst;
    }

    public List<VTuberGraduateData> GraduateVTubers(DictionaryRecord dictRecord, DateTime latestRecordTime, uint daysBefore, uint daysAfter)
    {
        List<VTuberGraduateData> rLst = new();

        DateTime _30DaysBefore = TodayDate.AddDays(-daysBefore);
        DateTime _30DaysAfter = TodayDate.AddDays(daysAfter);

        foreach (KeyValuePair<string, VTuberRecord> vtuberStatPair in dictRecord
            .Where(p => p.Value.Nationality.Contains(NationalityFilter))
            .Where(p => _30DaysBefore <= p.Value.GraduationDate && p.Value.GraduationDate < _30DaysAfter)
            .OrderByDescending(p => p.Value.GraduationDate))
        {
            string displayName = vtuberStatPair.Key;
            VTuberRecord record = vtuberStatPair.Value;

            ulong sub = record.YouTube.GetLatestSubscriberCount(latestRecordTime);
            bool hasDebut = TodayDate > record.DebutDate;

            VTuberGraduateData vTuberData = new(
                id: record.Id,
                activity: record.IsActive ? hasDebut ? Activity.active : Activity.preparing : Activity.graduate,
                name: record.DisplayName,
                imgUrl: record.ThumbnailUrl,
                YouTube: record.YouTube.ChannelId == "" ? null : new Types.YouTubeData() { id = record.YouTube.ChannelId, subscriberCount = sub == 0 ? null : sub },
                Twitch: record.Twitch.ChannelName == "" ? null : new Types.TwitchData() { id = record.Twitch.ChannelName, followerCount = record.Twitch.GetLatestFollowerCount(latestRecordTime) },
                popularVideo: GetPopularVideo(record, latestRecordTime),
                group: record.GroupName == "" ? null : record.GroupName,
                nationality: record.Nationality,
                graduateDate: record.GraduationDate.ToString("yyyy-MM-dd"));

            rLst.Add(vTuberData);
        }

        return rLst;
    }

    public List<VTuberPopularityData> TrendingVTubers(DictionaryRecord dictRecord, DateTime latestRecordTime, int? count)
    {
        List<VTuberPopularityData> rLst = new();

        foreach (KeyValuePair<string, VTuberRecord> vtuberStatPair in dictRecord
            .Where(p => p.Value.Nationality.Contains(NationalityFilter))
            .OrderByDescending(p => p, new VTuberRecordComparator.CombinedViewCount(latestRecordTime))
            .Take(count ?? int.MaxValue))
        {
            string displayName = vtuberStatPair.Key;
            VTuberRecord record = vtuberStatPair.Value;

            ulong sub = record.YouTube.GetLatestSubscriberCount(latestRecordTime);
            bool hasDebut = TodayDate > record.DebutDate;

            VTuberPopularityData vTuberData = new(
                id: record.Id,
                activity: record.IsActive ? hasDebut ? Activity.active : Activity.preparing : Activity.graduate,
                name: record.DisplayName,
                imgUrl: record.ThumbnailUrl,
                YouTube: record.YouTube.ChannelId == "" ? null : new YouTubePopularityData(
                    id: record.YouTube.ChannelId,
                    subscriberCount: sub == 0 ? null : sub,
                    popularity: record.YouTube.GetLatestRecentMedianViewCount(latestRecordTime)),
                Twitch: record.Twitch.ChannelName == "" ? null : new TwitchPopularityData(
                    id: record.Twitch.ChannelName,
                    followerCount: record.Twitch.GetLatestFollowerCount(latestRecordTime),
                    popularity: record.Twitch.GetLatestRecentMedianViewCount(latestRecordTime)),
                popularVideo: GetPopularVideo(record, latestRecordTime),
                group: record.GroupName == "" ? null : record.GroupName,
                nationality: record.Nationality);

            rLst.Add(vTuberData);
        }

        return rLst;
    }

    public List<GroupData> Groups(TrackList trackList, DictionaryRecord dictRecord, DateTime latestRecordTime)
    {
        List<GroupData> rLst = new();

        int id = 0;
        foreach (string groupName in trackList.GetGroupNameList())
        {
            ulong popularity = 0;
            List<Types.VTuberData> lstMembers = new();

            foreach (KeyValuePair<string, VTuberRecord> vtuberStatPair in dictRecord
                .Where(p => p.Value.Nationality.Contains(NationalityFilter))
                .Where(p => p.Value.GroupName == groupName)
                .OrderByDescending(p => p, new VTuberRecordComparator.CombinedCount(latestRecordTime)))
            {
                string displayName = vtuberStatPair.Key;
                VTuberRecord record = vtuberStatPair.Value;

                if (displayName == groupName)
                    continue;

                popularity += record.YouTube.GetLatestRecentMedianViewCount(latestRecordTime) + record.Twitch.GetLatestRecentMedianViewCount(latestRecordTime);

                ulong sub = record.YouTube.GetLatestSubscriberCount(latestRecordTime);
                bool hasDebut = TodayDate > record.DebutDate;

                Types.VTuberData vTuberData = new()
                {
                    id = record.Id,
                    activity = record.IsActive ? hasDebut ? Activity.active : Activity.preparing : Activity.graduate,
                    name = record.DisplayName,
                    imgUrl = record.ThumbnailUrl,
                    YouTube = record.YouTube.ChannelId == "" ? null : new Types.YouTubeData() { id = record.YouTube.ChannelId, subscriberCount = sub == 0 ? null : sub },
                    Twitch = record.Twitch.ChannelName == "" ? null : new Types.TwitchData() { id = record.Twitch.ChannelName, followerCount = record.Twitch.GetLatestFollowerCount(latestRecordTime) },
                    group = record.GroupName == "" ? null : record.GroupName,
                    nationality = record.Nationality,
                };

                lstMembers.Add(vTuberData);
            }

            if (lstMembers.Count == 0)
                continue;

            GroupData groupData = new()
            {
                id = id.ToString(),
                name = groupName,
                popularity = popularity,
                members = lstMembers,
            };

            rLst.Add(groupData);
            id++;
        }

        return rLst;
    }

    public Dictionary<string, List<Types.VTuberData>> GroupMembers(TrackList trackList, DictionaryRecord dictRecord, DateTime latestRecordTime)
    {
        Dictionary<string, List<Types.VTuberData>> rDict = new();

        foreach (string groupName in trackList.GetGroupNameList())
        {
            List<Types.VTuberData> lstMembers = new();

            foreach (KeyValuePair<string, VTuberRecord> vtuberStatPair in dictRecord
                .Where(p => p.Value.Nationality.Contains(NationalityFilter))
                .Where(p => p.Value.GroupName == groupName)
                .OrderByDescending(p => p, new VTuberRecordComparator.CombinedCount(latestRecordTime)))
            {
                string displayName = vtuberStatPair.Key;
                VTuberRecord record = vtuberStatPair.Value;

                ulong sub = record.YouTube.GetLatestSubscriberCount(latestRecordTime);
                bool hasDebut = TodayDate > record.DebutDate;

                Types.VTuberData vTuberData = new()
                {
                    id = record.Id,
                    activity = record.IsActive ? hasDebut ? Activity.active : Activity.preparing : Activity.graduate,
                    name = record.DisplayName == groupName ? record.DisplayName + "(官方頻道)" : record.DisplayName,
                    imgUrl = record.ThumbnailUrl,
                    YouTube = record.YouTube.ChannelId == "" ? null : new Types.YouTubeData() { id = record.YouTube.ChannelId, subscriberCount = sub == 0 ? null : sub },
                    Twitch = record.Twitch.ChannelName == "" ? null : new Types.TwitchData() { id = record.Twitch.ChannelName, followerCount = record.Twitch.GetLatestFollowerCount(latestRecordTime) },
                    popularVideo = GetPopularVideo(record, latestRecordTime),
                    group = record.GroupName == "" ? null : record.GroupName,
                    nationality = record.Nationality,
                };

                lstMembers.Add(vTuberData);
            }

            if (lstMembers.Count == 0)
                continue;

            rDict.Add(groupName, lstMembers);
        }

        return rDict;
    }
}
