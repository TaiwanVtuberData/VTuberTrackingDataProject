using Common.Types;
using GenerateJsonFile.Types;
using GenerateJsonFile.Utils;

namespace GenerateJsonFile;

class DictionaryRecordToJsonStruct
{
    private static readonly string DATE_FORMAT = "yyyy-MM-dd";

    private readonly DateTime TodayDate;
    private readonly DateTime LatestRecordTime;
    private readonly DataTransform dataTransform;
    private readonly string NationalityFilter;

    public DictionaryRecordToJsonStruct(DateTime todayDate, DateTime latestRecordTime, string nationalityFilter)
    {
        TodayDate = todayDate;
        LatestRecordTime = latestRecordTime;
        dataTransform = new(latestRecordTime);
        NationalityFilter = nationalityFilter;
    }

    public List<VTuberFullData> AllWithFullData(DictionaryRecord dictRecord)
    {
        List<Types.VTuberFullData> rLst = new();

        foreach (KeyValuePair<string, VTuberRecord> vtuberStatPair in dictRecord)
        {
            VTuberRecord record = vtuberStatPair.Value;

            VTuberFullData vTuberData = new(
                id: record.Id,
                activity: CommonActivityToJsonActivity(record.Activity),
                name: record.DisplayName,
                imgUrl: record.ImageUrl != null ? YouTubeImgUrlResize(record.ImageUrl, 88, 240) : null,
                YouTube: dataTransform.ToYouTubeData(record.YouTube),
                Twitch: dataTransform.ToTwitchData(record.Twitch),
                popularVideo: dataTransform.GetPopularVideo(record),
                group: record.GroupName,
                nationality: record.Nationality,
                debutDate: record.DebutDate?.ToString(DATE_FORMAT),
                graduateDate: record.GraduationDate?.ToString(DATE_FORMAT)
                );

            rLst.Add(vTuberData);
        }

        return rLst;
    }

    public List<Types.VTuberData> All(DictionaryRecord dictRecord, int? count)
    {
        List<Types.VTuberData> rLst = new();

        foreach (KeyValuePair<string, VTuberRecord> vtuberStatPair in dictRecord
            .Where(p => p.Value.Nationality.Contains(NationalityFilter))
            .OrderByDescending(p => p, new VTuberRecordComparator.CombinedCount(LatestRecordTime))
            .Take(count ?? int.MaxValue))
        {
            string displayName = vtuberStatPair.Key;
            VTuberRecord record = vtuberStatPair.Value;

            Types.VTuberData vTuberData = new(
                id: record.Id,
                activity: CommonActivityToJsonActivity(record.Activity),
                name: record.DisplayName,
                imgUrl: record.ImageUrl,
                YouTube: dataTransform.ToYouTubeData(record.YouTube),
                Twitch: dataTransform.ToTwitchData(record.Twitch),
                popularVideo: dataTransform.GetPopularVideo(record),
                group: record.GroupName,
                nationality: record.Nationality)
            ;

            rLst.Add(vTuberData);
        }

        return rLst;
    }

    public List<VTuberGrowthData> GrowingVTubers(DictionaryRecord dictRecord, int? count)
    {
        Dictionary<string, YouTubeGrowthData> dictGrowth = new(dictRecord.Count);

        foreach (KeyValuePair<string, VTuberRecord> vtuberStatPair in dictRecord)
        {
            string id = vtuberStatPair.Key;
            VTuberRecord record = vtuberStatPair.Value;

            if (record.YouTube == null)
            {
                continue;
            }

            DictionaryRecord.GetGrowthResult _7DaysResult = dictRecord.GetYouTubeSubscriberCountGrowth(id, days: 7, daysLimit: 1, out long _7DaysGrowth, out decimal _7DaysGrowthRate);
            DictionaryRecord.GetGrowthResult _30DaysResult = dictRecord.GetYouTubeSubscriberCountGrowth(id, days: 30, daysLimit: 7, out long _30DaysGrowth, out decimal _30DaysGrowthRate);

            YouTubeGrowthData growthData = new(
                id: record.YouTube.ChannelId,
                subscriber: dataTransform.ToYouTubeSubscriber(record.YouTube),
                _7DaysGrowth: new GrowthData(diff: _7DaysGrowth, recordType: GetGrowthResultToString(_7DaysResult)),
                _30DaysGrowth: new GrowthData(diff: _30DaysGrowth, recordType: GetGrowthResultToString(_30DaysResult)),
                Nationality: record.Nationality);

            dictGrowth.Add(id, growthData);
        }

        List<VTuberGrowthData> rLst = new();

        foreach (KeyValuePair<string, YouTubeGrowthData> growthPair in dictGrowth
            .Where(p => p.Value.Nationality != null && p.Value.Nationality.Contains(NationalityFilter))
            .Where(p => p.Value.subscriber.tag == CountTag.has)
            .Where(p => p.Value._7DaysGrowth.diff >= 100)
            .Where(p => dictRecord[p.Key].YouTube != null)
            .OrderByDescending(p => ToGrowthPercentage(p.Value))
            .Take(count ?? int.MaxValue))
        {
            string id = growthPair.Key;
            YouTubeGrowthData youTubeGrowthData = growthPair.Value;

            VTuberRecord record = dictRecord[id];

            VTuberGrowthData vTuberData = new(
                id: record.Id,
                activity: CommonActivityToJsonActivity(record.Activity),
                name: record.DisplayName,
                imgUrl: record.ImageUrl,
                YouTube: new YouTubeGrowthData(
                id: youTubeGrowthData.id,
                subscriber: youTubeGrowthData.subscriber,
                _7DaysGrowth: youTubeGrowthData._7DaysGrowth,
                _30DaysGrowth: youTubeGrowthData._30DaysGrowth,
                Nationality: null),
                Twitch: dataTransform.ToTwitchData(record.Twitch),
                popularVideo: dataTransform.GetPopularVideo(record),
                group: record.GroupName,
                nationality: record.Nationality
            );

            rLst.Add(vTuberData);
        }

        return rLst;
    }

    private class _7DaysGrowthComparer : IComparer<YouTubeViewCountGrowthData>
    {
        public int Compare(YouTubeViewCountGrowthData? a, YouTubeViewCountGrowthData? b)
        {
            if (a == null || b == null)
                return 0;

            return a._7DaysGrowth.diff.CompareTo(b._7DaysGrowth.diff);
        }
    }

    private class _30DaysGrowthComparer : IComparer<YouTubeViewCountGrowthData>
    {
        public int Compare(YouTubeViewCountGrowthData? a, YouTubeViewCountGrowthData? b)
        {
            if (a == null || b == null)
                return 0;

            return a._30DaysGrowth.diff.CompareTo(b._30DaysGrowth.diff);
        }
    }

    private static IComparer<YouTubeViewCountGrowthData> GetSortFunction(SortBy sortBy)
    {
        return sortBy switch
        {
            SortBy._30Days => new _30DaysGrowthComparer(),
            _ => new _7DaysGrowthComparer(),
        };
    }

    public List<VTuberViewCountGrowthData> VTubersViewCountChange(DictionaryRecord dictRecord, SortBy sortBy, int? count)
    {
        Dictionary<string, YouTubeViewCountGrowthData> dictGrowth = new(dictRecord.Count);

        foreach (KeyValuePair<string, VTuberRecord> vtuberStatPair in dictRecord)
        {
            string id = vtuberStatPair.Key;
            VTuberRecord record = vtuberStatPair.Value;

            if (record.YouTube == null)
            {
                continue;
            }

            DictionaryRecord.GetGrowthResult _7DaysResult = dictRecord.GetYouTubeViewCountGrowth(id, days: 7, daysLimit: 1, out decimal _7DaysGrowth, out decimal _7DaysGrowthRate);
            DictionaryRecord.GetGrowthResult _30DaysResult = dictRecord.GetYouTubeViewCountGrowth(id, days: 30, daysLimit: 7, out decimal _30DaysGrowth, out decimal _30DaysGrowthRate);

            YouTubeViewCountGrowthData growthData = new(
                id: record.YouTube.ChannelId,
                totalViewCount: dataTransform.ToYouTubeTotalViewCount(record.YouTube),
                _7DaysGrowth: new GrowthData(diff: _7DaysGrowth, recordType: GetGrowthResultToString(_7DaysResult)),
                _30DaysGrowth: new GrowthData(diff: _30DaysGrowth, recordType: GetGrowthResultToString(_30DaysResult)),
                Nationality: record.Nationality
            );

            dictGrowth.Add(id, growthData);
        }

        List<VTuberViewCountGrowthData> rLst = new();

        foreach (KeyValuePair<string, YouTubeViewCountGrowthData> growthPair in dictGrowth
            .Where(p => p.Value.Nationality != null && p.Value.Nationality.Contains(NationalityFilter))
            .Where(p => p.Value.totalViewCount != 0)
            .Where(p => p.Value._7DaysGrowth.diff >= 0)
            .Where(p => dictRecord[p.Key].YouTube != null)
            .OrderByDescending(p => p.Value, GetSortFunction(sortBy))
            .Take(count ?? int.MaxValue))
        {
            string id = growthPair.Key;
            YouTubeViewCountGrowthData youTubeGrowthData = growthPair.Value;

            VTuberRecord record = dictRecord[id];

            VTuberViewCountGrowthData vTuberData = new(
                id: record.Id,
                activity: CommonActivityToJsonActivity(record.Activity),
                name: record.DisplayName,
                imgUrl: record.ImageUrl,
                YouTube: new YouTubeViewCountGrowthData(
                    id: youTubeGrowthData.id,
                    totalViewCount: youTubeGrowthData.totalViewCount,
                    _7DaysGrowth: youTubeGrowthData._7DaysGrowth,
                    _30DaysGrowth: youTubeGrowthData._30DaysGrowth,
                    Nationality: null
                ),
                Twitch: dataTransform.ToTwitchData(record.Twitch),
                popularVideo: dataTransform.GetPopularVideo(record),
                group: record.GroupName,
                nationality: record.Nationality
            );

            rLst.Add(vTuberData);
        }

        return rLst;
    }

    public List<VTuberDebutData> DebutVTubers(DictionaryRecord dictRecord, uint daysBefore, uint daysAfter)
    {
        List<VTuberDebutData> rLst = new();

        DateTime _30DaysBefore = TodayDate.AddDays(-daysBefore);
        DateTime _30DaysAfter = TodayDate.AddDays(daysAfter);


        foreach (KeyValuePair<string, VTuberRecord> vtuberStatPair in dictRecord
            .Where(p => p.Value.Nationality.Contains(NationalityFilter))
            .Where(p => p.Value.DebutDate.HasValue)
            .Where(p => IsBetween(p.Value.DebutDate, _30DaysBefore, _30DaysAfter))
            .OrderByDescending(p => p.Value.DebutDate))
        {
            string displayName = vtuberStatPair.Key;
            VTuberRecord record = vtuberStatPair.Value;

            if (record.DebutDate is null)
            {
                continue;
            }

            VTuberDebutData vTuberData = new(
                id: record.Id,
                activity: CommonActivityToJsonActivity(record.Activity),
                name: record.DisplayName,
                imgUrl: record.ImageUrl,
                YouTube: dataTransform.ToYouTubeData(record.YouTube),
                Twitch: dataTransform.ToTwitchData(record.Twitch),
                popularVideo: dataTransform.GetPopularVideo(record),
                group: record.GroupName,
                nationality: record.Nationality,
                debutDate: record.DebutDate.Value.ToString(DATE_FORMAT)
                );

            rLst.Add(vTuberData);
        }

        return rLst;
    }

    public List<VTuberGraduateData> GraduateVTubers(DictionaryRecord dictRecord, uint daysBefore, uint daysAfter)
    {
        List<VTuberGraduateData> rLst = new();

        DateTime _30DaysBefore = TodayDate.AddDays(-daysBefore);
        DateTime _30DaysAfter = TodayDate.AddDays(daysAfter);

        foreach (KeyValuePair<string, VTuberRecord> vtuberStatPair in dictRecord
            .Where(p => p.Value.Nationality.Contains(NationalityFilter))
            .Where(p => p.Value.GraduationDate.HasValue)
            .Where(p => IsBetween(p.Value.GraduationDate, _30DaysBefore, _30DaysAfter))
            .OrderByDescending(p => p.Value.GraduationDate))
        {
            string displayName = vtuberStatPair.Key;
            VTuberRecord record = vtuberStatPair.Value;

            if (record.GraduationDate is null)
            {
                continue;
            }

            VTuberGraduateData vTuberData = new(
                id: record.Id,
                activity: CommonActivityToJsonActivity(record.Activity),
                name: record.DisplayName,
                imgUrl: record.ImageUrl,
                YouTube: dataTransform.ToYouTubeData(record.YouTube),
                Twitch: dataTransform.ToTwitchData(record.Twitch),
                popularVideo: dataTransform.GetPopularVideo(record),
                group: record.GroupName,
                nationality: record.Nationality,
                graduateDate: record.GraduationDate.Value.ToString(DATE_FORMAT)
                );

            rLst.Add(vTuberData);
        }

        return rLst;
    }

    public List<VTuberPopularityData> TrendingVTubers(DictionaryRecord dictRecord, int? count)
    {
        List<VTuberPopularityData> rLst = new();

        foreach (KeyValuePair<string, VTuberRecord> vtuberStatPair in dictRecord
            .Where(p => p.Value.Nationality.Contains(NationalityFilter))
            .OrderByDescending(p => p, new VTuberRecordComparator.CombinedViewCount(LatestRecordTime))
            .Take(count ?? int.MaxValue))
        {
            string displayName = vtuberStatPair.Key;
            VTuberRecord record = vtuberStatPair.Value;

            VTuberPopularityData vTuberData = new(
                id: record.Id,
                activity: CommonActivityToJsonActivity(record.Activity),
                name: record.DisplayName,
                imgUrl: record.ImageUrl,
                YouTube: dataTransform.ToYouTubePopularityData(record.YouTube),
                Twitch: dataTransform.ToTwitchPopularityData(record.Twitch),
                popularVideo: dataTransform.GetPopularVideo(record),
                group: record.GroupName,
                nationality: record.Nationality);

            rLst.Add(vTuberData);
        }

        return rLst;
    }

    public List<GroupData> Groups(TrackList trackList, DictionaryRecord dictRecord)
    {
        List<GroupData> rLst = new();

        foreach (string groupName in trackList.GetGroupNameList())
        {
            ulong popularity = 0;
            List<Types.VTuberData> lstMembers = new();

            foreach (KeyValuePair<string, VTuberRecord> vtuberStatPair in dictRecord
                .Where(p => p.Value.Nationality.Contains(NationalityFilter))
                .Where(p => p.Value.GroupName == groupName)
                .OrderByDescending(p => p, new VTuberRecordComparator.CombinedCount(LatestRecordTime)))
            {
                string displayName = vtuberStatPair.Key;
                VTuberRecord record = vtuberStatPair.Value;

                if (displayName == groupName)
                    continue;

                popularity += dataTransform.ToCombinedPopularity(record);

                Types.VTuberData vTuberData = new(
                    id: record.Id,
                    activity: CommonActivityToJsonActivity(record.Activity),
                    name: record.DisplayName,
                    imgUrl: record.ImageUrl,
                    YouTube: dataTransform.ToYouTubeData(record.YouTube),
                    Twitch: dataTransform.ToTwitchData(record.Twitch),
                    popularVideo: dataTransform.GetPopularVideo(record),
                    group: record.GroupName,
                    nationality: record.Nationality
                );

                lstMembers.Add(vTuberData);
            }

            if (lstMembers.Count == 0)
                continue;

            GroupData groupData = new()
            {
                id = groupName,
                name = groupName,
                popularity = popularity,
                members = lstMembers,
            };

            rLst.Add(groupData);
        }

        return rLst;
    }

    public Dictionary<string, List<Types.VTuberData>> GroupMembers(TrackList trackList, DictionaryRecord dictRecord)
    {
        Dictionary<string, List<Types.VTuberData>> rDict = new();

        foreach (string groupName in trackList.GetGroupNameList())
        {
            List<Types.VTuberData> lstMembers = new();

            foreach (KeyValuePair<string, VTuberRecord> vtuberStatPair in dictRecord
                .Where(p => p.Value.Nationality.Contains(NationalityFilter))
                .Where(p => p.Value.GroupName == groupName)
                .OrderByDescending(p => p, new VTuberRecordComparator.CombinedCount(LatestRecordTime)))
            {
                string displayName = vtuberStatPair.Key;
                VTuberRecord record = vtuberStatPair.Value;

                Types.VTuberData vTuberData = new(
                    id: record.Id,
                    activity: CommonActivityToJsonActivity(record.Activity),
                    name: record.DisplayName == groupName ? record.DisplayName + "(官方頻道)" : record.DisplayName,
                    imgUrl: record.ImageUrl,
                    YouTube: dataTransform.ToYouTubeData(record.YouTube),
                    Twitch: dataTransform.ToTwitchData(record.Twitch),
                    popularVideo: dataTransform.GetPopularVideo(record),
                    group: record.GroupName,
                    nationality: record.Nationality
                );

                lstMembers.Add(vTuberData);
            }

            if (lstMembers.Count == 0)
                continue;

            rDict.Add(groupName, lstMembers);
        }

        return rDict;
    }

    private static string YouTubeImgUrlResize(string YouTubeImgUrl, int origSize, int newSize)
    {
        return YouTubeImgUrl.Replace($"=s{origSize}", $"=s{newSize}");
    }

    private static Types.Activity CommonActivityToJsonActivity(Common.Types.Activity commonActivity)
    {
        return commonActivity switch
        {
            Common.Types.Activity.Preparing => Types.Activity.preparing,
            Common.Types.Activity.Active => Types.Activity.active,
            Common.Types.Activity.Graduated => Types.Activity.graduate,
            _ => throw new InvalidOperationException(),
        };
    }

    private static GrowthRecordType GetGrowthResultToString(DictionaryRecord.GetGrowthResult getGrowthResult)
    {
        return getGrowthResult switch
        {
            DictionaryRecord.GetGrowthResult.Found => GrowthRecordType.full,
            DictionaryRecord.GetGrowthResult.NotExact => GrowthRecordType.partial,
            _ => GrowthRecordType.none,
        };
    }

    private static decimal ToGrowthPercentage(YouTubeGrowthData growthData)
    {
        if (growthData.subscriber.tag != CountTag.has)
        {
            return decimal.MinValue;
        }

        // subscriber should be HasCountType now
        return growthData.subscriber.count.HasValue ? ((growthData._7DaysGrowth.diff) / growthData.subscriber.count.Value) : decimal.MinValue;
    }

    private static bool IsBetween(DateOnly? date, DateTime _30DaysBefore, DateTime _30DaysAfter)
    {
        if (date.HasValue)
            return _30DaysBefore <= date.Value.ToDateTime(TimeOnly.MinValue) && date.Value.ToDateTime(TimeOnly.MinValue) < _30DaysAfter;

        return false;
    }
}
