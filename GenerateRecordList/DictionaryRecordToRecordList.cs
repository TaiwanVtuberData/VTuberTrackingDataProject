﻿using Common.Types;
using Common.Types.Basic;
using GenerateRecordList.Types;
using GenerateRecordList.Utils;

namespace GenerateRecordList;

public class DictionaryRecordToRecordList
{
    private readonly TrackList _trackList;
    private readonly DictionaryRecord DictRecord;
    private readonly DateOnly TodayDate;
    private readonly DateTimeOffset LatestRecordTime;
    private readonly DateTimeOffset LatestBasicDataTime;
    private readonly DataTransform dataTransform;
    private readonly string NationalityFilter;

    public DictionaryRecordToRecordList(
        TrackList trackList,
        DictionaryRecord dictRecord,
        DateOnly todayDate,
        DateTimeOffset latestRecordTime,
        DateTimeOffset latestBasicDataTime,
        string nationalityFilter
    )
    {
        _trackList = trackList;
        DictRecord = dictRecord;
        TodayDate = todayDate;
        LatestRecordTime = latestRecordTime;
        LatestBasicDataTime = latestBasicDataTime;
        dataTransform = new(LatestRecordTime, LatestBasicDataTime);
        NationalityFilter = nationalityFilter;
    }

    public List<VTuberFullData> AllWithFullData(LiveVideosList liveVideosList)
    {
        List<VTuberFullData> rLst = [];

        foreach (KeyValuePair<VTuberId, VTuberRecord> vtuberStatPair in DictRecord)
        {
            VTuberRecord record = vtuberStatPair.Value;

            List<VTuberLivestreamData> livestreams = liveVideosList
                .Where(e => e.Id == record.Id)
                .Select(e => new VTuberLivestreamData(
                    title: e.Title,
                    videoUrl: e.Url,
                    thumbnailUrl: MiscUtils.SetTwitchLivestreamThumbnailUrlSize(
                        e.ThumbnailUrl,
                        width: 178,
                        height: 100
                    ),
                    startTime: e.PublishDateTime != DateTime.UnixEpoch
                        ? MiscUtils.ToIso8601UtcString(e.PublishDateTime)
                        : null
                ))
                .OrderBy(e => e.startTime)
                .ToList();

            VTuberFullData vTuberData =
                new(
                    id: record.Id,
                    activity: CommonActivityToJsonActivity(record.Activity),
                    name: record.DisplayName,
                    imgUrl: record.ImageUrl != null
                        ? YouTubeImgUrlResize(record.ImageUrl, 88, 240)
                        : null,
                    YouTube: dataTransform.ToYouTubeData(record.YouTube),
                    Twitch: dataTransform.ToTwitchData(record.Twitch),
                    popularVideo: dataTransform.GetPopularVideo(record),
                    group: record.GroupName,
                    nationality: record.Nationality,
                    debutDate: record.DebutDate?.ToString(Constant.DATE_FORMAT),
                    graduateDate: record.GraduationDate?.ToString(Constant.DATE_FORMAT),
                    livestreams: livestreams
                );

            rLst.Add(vTuberData);
        }

        return rLst;
    }

    public List<Types.VTuberData> All(int? count)
    {
        List<Types.VTuberData> rLst = [];

        foreach (
            KeyValuePair<VTuberId, VTuberRecord> vtuberStatPair in DictRecord
                .Where(p => p.Value.Nationality.Contains(NationalityFilter))
                .OrderByDescending(
                    p => p,
                    new VTuberRecordComparator.CombinedCount(LatestBasicDataTime)
                )
                .Take(count ?? int.MaxValue)
        )
        {
            VTuberRecord record = vtuberStatPair.Value;

            Types.VTuberData vTuberData =
                new(
                    id: record.Id,
                    activity: CommonActivityToJsonActivity(record.Activity),
                    name: record.DisplayName,
                    imgUrl: record.ImageUrl,
                    YouTube: dataTransform.ToYouTubeData(record.YouTube),
                    Twitch: dataTransform.ToTwitchData(record.Twitch),
                    popularVideo: dataTransform.GetPopularVideo(record),
                    group: record.GroupName,
                    nationality: record.Nationality,
                    debutDate: record.DebutDate?.ToString(Constant.DATE_FORMAT)
                );

            rLst.Add(vTuberData);
        }

        return rLst;
    }

    public List<VTuberGrowthData> GrowingVTubers(int? count, int growthLimit = 100)
    {
        Dictionary<VTuberId, YouTubeGrowthData> dictGrowth = new(DictRecord.Count);

        foreach (KeyValuePair<VTuberId, VTuberRecord> vtuberStatPair in DictRecord)
        {
            VTuberId id = vtuberStatPair.Key;
            VTuberRecord record = vtuberStatPair.Value;

            if (record.YouTube == null)
            {
                continue;
            }

            DictionaryRecord.GrowthResult _7DaysResult = DictRecord.GetYouTubeSubscriberCountGrowth(
                id,
                days: 7,
                daysLimit: 1
            );
            DictionaryRecord.GrowthResult _30DaysResult =
                DictRecord.GetYouTubeSubscriberCountGrowth(id, days: 30, daysLimit: 7);

            YouTubeGrowthData growthData =
                new(
                    id: record.YouTube.ChannelId,
                    subscriber: dataTransform.ToYouTubeSubscriber(record.YouTube),
                    _7DaysGrowth: new GrowthData(
                        diff: _7DaysResult.Growth,
                        recordType: GetGrowthResultToString(_7DaysResult.GrowthType)
                    ),
                    _30DaysGrowth: new GrowthData(
                        diff: _30DaysResult.Growth,
                        recordType: GetGrowthResultToString(_30DaysResult.GrowthType)
                    ),
                    Nationality: record.Nationality
                );

            dictGrowth.Add(id, growthData);
        }

        List<VTuberGrowthData> rLst = [];

        foreach (
            KeyValuePair<VTuberId, YouTubeGrowthData> growthPair in dictGrowth
                .Where(p =>
                    p.Value.Nationality != null && p.Value.Nationality.Contains(NationalityFilter)
                )
                .Where(p => p.Value.subscriber.tag == CountTag.has)
                .Where(p => p.Value._7DaysGrowth.diff >= growthLimit)
                .Where(p => DictRecord[p.Key].YouTube != null)
                .OrderByDescending(p => ToGrowthPercentage(p.Value))
                .Take(count ?? int.MaxValue)
        )
        {
            VTuberId id = growthPair.Key;
            YouTubeGrowthData youTubeGrowthData = growthPair.Value;

            VTuberRecord record = DictRecord[id];

            VTuberGrowthData vTuberData =
                new(
                    id: record.Id,
                    activity: CommonActivityToJsonActivity(record.Activity),
                    name: record.DisplayName,
                    imgUrl: record.ImageUrl,
                    YouTube: new YouTubeGrowthData(
                        id: youTubeGrowthData.id,
                        subscriber: youTubeGrowthData.subscriber,
                        _7DaysGrowth: youTubeGrowthData._7DaysGrowth,
                        _30DaysGrowth: youTubeGrowthData._30DaysGrowth,
                        Nationality: null
                    ),
                    Twitch: dataTransform.ToTwitchData(record.Twitch),
                    popularVideo: dataTransform.GetPopularVideo(record),
                    group: record.GroupName,
                    nationality: record.Nationality,
                    debutDate: record.DebutDate?.ToString(Constant.DATE_FORMAT)
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

    public List<VTuberViewCountGrowthData> VTubersViewCountChange(SortBy sortBy, int? count)
    {
        Dictionary<VTuberId, YouTubeViewCountGrowthData> dictGrowth = new(DictRecord.Count);

        foreach (KeyValuePair<VTuberId, VTuberRecord> vtuberStatPair in DictRecord)
        {
            VTuberId id = vtuberStatPair.Key;
            VTuberRecord record = vtuberStatPair.Value;

            if (record.YouTube == null)
            {
                continue;
            }

            DictionaryRecord.GrowthResult _7DaysResult = DictRecord.GetYouTubeViewCountGrowth(
                id,
                days: 7,
                daysLimit: 1
            );
            DictionaryRecord.GrowthResult _30DaysResult = DictRecord.GetYouTubeViewCountGrowth(
                id,
                days: 30,
                daysLimit: 7
            );

            YouTubeViewCountGrowthData growthData =
                new(
                    id: record.YouTube.ChannelId,
                    totalViewCount: dataTransform.ToYouTubeTotalViewCount(record.YouTube),
                    _7DaysGrowth: new GrowthData(
                        diff: _7DaysResult.Growth,
                        recordType: GetGrowthResultToString(_7DaysResult.GrowthType)
                    ),
                    _30DaysGrowth: new GrowthData(
                        diff: _30DaysResult.Growth,
                        recordType: GetGrowthResultToString(_30DaysResult.GrowthType)
                    ),
                    Nationality: record.Nationality
                );

            dictGrowth.Add(id, growthData);
        }

        List<VTuberViewCountGrowthData> rLst = [];

        foreach (
            KeyValuePair<VTuberId, YouTubeViewCountGrowthData> growthPair in dictGrowth
                .Where(p =>
                    p.Value.Nationality != null && p.Value.Nationality.Contains(NationalityFilter)
                )
                .Where(p => p.Value.totalViewCount != 0)
                .Where(p => p.Value._7DaysGrowth.diff >= 0)
                .Where(p => DictRecord[p.Key].YouTube != null)
                .OrderByDescending(p => p.Value, GetSortFunction(sortBy))
                .Take(count ?? int.MaxValue)
        )
        {
            VTuberId id = growthPair.Key;
            YouTubeViewCountGrowthData youTubeGrowthData = growthPair.Value;

            VTuberRecord record = DictRecord[id];

            VTuberViewCountGrowthData vTuberData =
                new(
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

    public List<VTuberDebutData> DebutVTubers(uint daysBefore, uint daysAfter)
    {
        List<VTuberDebutData> rLst = [];

        DateOnly dateBefore = TodayDate.AddDays(-(int)daysBefore);
        DateOnly dateAfter = TodayDate.AddDays((int)daysAfter);

        foreach (
            KeyValuePair<VTuberId, VTuberRecord> vtuberStatPair in DictRecord
                .Where(p => p.Value.Nationality.Contains(NationalityFilter))
                .Where(p => p.Value.DebutDate.HasValue)
                .Where(p => TimeUtils.IsBetween(p.Value.DebutDate, dateBefore, dateAfter))
                .OrderByDescending(p => p.Value.DebutDate)
        )
        {
            VTuberRecord record = vtuberStatPair.Value;

            if (record.DebutDate is null)
            {
                continue;
            }

            VTuberDebutData vTuberData =
                new(
                    id: record.Id,
                    activity: CommonActivityToJsonActivity(record.Activity),
                    name: record.DisplayName,
                    imgUrl: record.ImageUrl,
                    YouTube: dataTransform.ToYouTubeData(record.YouTube),
                    Twitch: dataTransform.ToTwitchData(record.Twitch),
                    popularVideo: dataTransform.GetPopularVideo(record),
                    group: record.GroupName,
                    nationality: record.Nationality,
                    debutDate: record.DebutDate.Value.ToString(Constant.DATE_FORMAT)
                );

            rLst.Add(vTuberData);
        }

        return rLst;
    }

    public List<VTuberAnniversaryData> AnniversaryVTubers(uint daysBefore, uint daysAfter)
    {
        List<VTuberAnniversaryData> rLst = [];

        DateOnly dateBefore = TodayDate.AddDays(-(int)daysBefore);
        DateOnly dateAfter = TodayDate.AddDays((int)daysAfter);

        foreach (
            KeyValuePair<VTuberId, VTuberRecord> vtuberStatPair in DictRecord
                .Where(p => p.Value.Nationality.Contains(NationalityFilter))
                .Where(p => p.Value.DebutDate.HasValue)
                // only active VTubers are listed
                .Where(p => p.Value.Activity == Common.Types.Activity.Active)
                .OrderByDescending(p => p.Value.DebutDate)
        )
        {
            VTuberRecord record = vtuberStatPair.Value;

            if (record.DebutDate is null)
            {
                continue;
            }

            if (
                TimeUtils.GetAnniversaryYearByRange(
                    maybeDate: record.DebutDate,
                    dateBefore: dateBefore,
                    dateAfter: dateAfter,
                    anniversaryYearCount: out uint anniversaryYearCount
                )
            )
            {
                // if true, do nothing
            }
            else
            {
                continue;
            }

            VTuberAnniversaryData vTuberData =
                new(
                    id: record.Id,
                    activity: CommonActivityToJsonActivity(record.Activity),
                    name: record.DisplayName,
                    imgUrl: record.ImageUrl,
                    YouTube: dataTransform.ToYouTubeData(record.YouTube),
                    Twitch: dataTransform.ToTwitchData(record.Twitch),
                    popularVideo: dataTransform.GetPopularVideo(record),
                    group: record.GroupName,
                    nationality: record.Nationality,
                    debutDate: record.DebutDate.Value.ToString(Constant.DATE_FORMAT),
                    debutDateOfTheYear: record.DebutDate.Value.ToString(
                        Constant.DATE_OF_THE_YEAR_FORMAT
                    ),
                    anniversaryYearCount: anniversaryYearCount
                );

            rLst.Add(vTuberData);
        }

        return rLst;
    }

    public List<VTuberGraduateData> GraduateVTubers(uint daysBefore, uint daysAfter)
    {
        List<VTuberGraduateData> rLst = [];

        DateOnly dateBefore = TodayDate.AddDays(-(int)daysBefore);
        DateOnly dateAfter = TodayDate.AddDays((int)daysAfter);

        foreach (
            KeyValuePair<VTuberId, VTuberRecord> vtuberStatPair in DictRecord
                .Where(p => p.Value.Nationality.Contains(NationalityFilter))
                .Where(p => p.Value.GraduationDate.HasValue)
                .Where(p => TimeUtils.IsBetween(p.Value.GraduationDate, dateBefore, dateAfter))
                .OrderByDescending(p => p.Value.GraduationDate)
        )
        {
            VTuberRecord record = vtuberStatPair.Value;

            if (record.GraduationDate is null)
            {
                continue;
            }

            VTuberGraduateData vTuberData =
                new(
                    id: record.Id,
                    activity: CommonActivityToJsonActivity(record.Activity),
                    name: record.DisplayName,
                    imgUrl: record.ImageUrl,
                    YouTube: dataTransform.ToYouTubeData(record.YouTube),
                    Twitch: dataTransform.ToTwitchData(record.Twitch),
                    popularVideo: dataTransform.GetPopularVideo(record),
                    group: record.GroupName,
                    nationality: record.Nationality,
                    debutDate: record.DebutDate?.ToString(Constant.DATE_FORMAT),
                    graduateDate: record.GraduationDate.Value.ToString(Constant.DATE_FORMAT)
                );

            rLst.Add(vTuberData);
        }

        return rLst;
    }

    private Func<VTuberRecord.YouTubeData?, YouTubePopularityData?> GetYouTubePopularityFunction(
        TrendingVTuberSortOrder sortBy
    )
    {
        return sortBy switch
        {
            TrendingVTuberSortOrder.livestream => (VTuberRecord.YouTubeData? input) =>
                dataTransform.ToYouTubeLivestreamPopularityData(input),
            TrendingVTuberSortOrder.video => (VTuberRecord.YouTubeData? input) =>
                dataTransform.ToYouTubeVideoPopularityData(input),
            _ => (VTuberRecord.YouTubeData? input) =>
                dataTransform.ToYouTubeTotalPopularityData(input),
        };
    }

    private Func<VTuberRecord.TwitchData?, TwitchPopularityData?> GetTwitchPopularityFunction(
        TrendingVTuberSortOrder sortBy
    )
    {
        return sortBy switch
        {
            TrendingVTuberSortOrder.video => (VTuberRecord.TwitchData? input) =>
            {
                TwitchPopularityData? result = dataTransform.ToTwitchPopularityData(input);

                if (result is null)
                {
                    return null;
                }

                result = result with { popularity = 0 };

                return result;
            },
            _ => (VTuberRecord.TwitchData? input) => dataTransform.ToTwitchPopularityData(input),
        };
    }

    public List<VTuberPopularityData> TrendingVTubers(TrendingVTuberSortOrder sortBy, int? count)
    {
        List<VTuberPopularityData> rLst = [];

        Func<VTuberRecord.YouTubeData?, YouTubePopularityData?> youTubePopularityFunc =
            GetYouTubePopularityFunction(sortBy);
        Func<VTuberRecord.TwitchData?, TwitchPopularityData?> twitchPopularityFunc =
            GetTwitchPopularityFunction(sortBy);

        foreach (
            KeyValuePair<VTuberId, VTuberRecord> vtuberStatPair in DictRecord.Where(p =>
                p.Value.Nationality.Contains(NationalityFilter)
            )
        )
        {
            VTuberRecord record = vtuberStatPair.Value;

            VTuberPopularityData vTuberData =
                new(
                    id: record.Id,
                    activity: CommonActivityToJsonActivity(record.Activity),
                    name: record.DisplayName,
                    imgUrl: record.ImageUrl,
                    YouTube: youTubePopularityFunc(record.YouTube),
                    Twitch: twitchPopularityFunc(record.Twitch),
                    popularVideo: dataTransform.GetPopularVideo(record),
                    group: record.GroupName,
                    nationality: record.Nationality,
                    debutDate: record.DebutDate?.ToString(Constant.DATE_FORMAT)
                );

            rLst.Add(vTuberData);
        }

        return rLst.OrderByDescending(e =>
                (e.YouTube?.popularity ?? 0) + (e.Twitch?.popularity ?? 0)
            )
            .Take(count ?? int.MaxValue)
            .ToList();
    }

    public List<VTuberSubscriberCountToPopularityData> YouTubeSubscriberCountToPopularity(
        TrendingVTuberSortOrder sortBy,
        int? count
    )
    {
        List<VTuberSubscriberCountToPopularityData> rLst = [];

        Func<
            VTuberRecord.YouTubeData?,
            YouTubeSubscriberCountToPopularityData?
        > youTubePopularityFunc = GetYouTubeSubscriberCountToPopularityFunction(sortBy);
        Func<VTuberRecord.TwitchData?, TwitchPopularityData?> twitchPopularityFunc =
            GetTwitchPopularityFunction(sortBy);

        foreach (
            KeyValuePair<VTuberId, VTuberRecord> vtuberStatPair in DictRecord
                .Where(p => p.Value.Nationality.Contains(NationalityFilter))
                .Where(p =>
                    p.Value?.YouTube?.GetBasicDataOrLatest(LatestBasicDataTime)?.SubscriberCount
                    >= 2000
                )
        )
        {
            VTuberRecord record = vtuberStatPair.Value;

            VTuberSubscriberCountToPopularityData vTuberData =
                new(
                    id: record.Id,
                    activity: CommonActivityToJsonActivity(record.Activity),
                    name: record.DisplayName,
                    imgUrl: record.ImageUrl,
                    YouTube: youTubePopularityFunc(record.YouTube),
                    Twitch: twitchPopularityFunc(record.Twitch),
                    popularVideo: dataTransform.GetPopularVideo(record),
                    group: record.GroupName,
                    nationality: record.Nationality,
                    debutDate: record.DebutDate?.ToString(Constant.DATE_FORMAT)
                );

            rLst.Add(vTuberData);
        }

        return rLst.OrderByDescending(e => (e.YouTube?.popularity ?? 0))
            .Take(count ?? int.MaxValue)
            .ToList();
    }

    private Func<
        VTuberRecord.YouTubeData?,
        YouTubeSubscriberCountToPopularityData?
    > GetYouTubeSubscriberCountToPopularityFunction(TrendingVTuberSortOrder sortBy)
    {
        return sortBy switch
        {
            TrendingVTuberSortOrder.livestream => (VTuberRecord.YouTubeData? input) =>
                dataTransform.ToYouTubeSubscriberCountToLivestreamPopularity(input),
            TrendingVTuberSortOrder.video => (VTuberRecord.YouTubeData? input) =>
                dataTransform.ToYouTubeSubscriberCountToVideoPopularity(input),
            _ => (VTuberRecord.YouTubeData? input) =>
                dataTransform.ToYouTubeSubscriberCountToTotalPopularity(input),
        };
    }

    public List<GroupData> Groups()
    {
        List<GroupData> rLst = [];

        foreach (string groupName in _trackList.GetGroupNameList())
        {
            ulong totalPopularity = 0;
            ulong livestreamPopularity = 0;
            ulong videoPopularity = 0;
            List<Types.VTuberData> lstMembers = [];

            foreach (
                KeyValuePair<VTuberId, VTuberRecord> vtuberStatPair in DictRecord
                    .Where(p => p.Value.Nationality.Contains(NationalityFilter))
                    .Where(p => p.Value.GroupName == groupName)
                    .OrderByDescending(
                        p => p,
                        new VTuberRecordComparator.CombinedCount(LatestBasicDataTime)
                    )
            )
            {
                VTuberId id = vtuberStatPair.Key;
                VTuberRecord record = vtuberStatPair.Value;

                if (id.Value == groupName)
                    continue;

                totalPopularity += dataTransform.ToCombinedTotalPopularity(record);
                livestreamPopularity += dataTransform.ToCombinedLivestreamPopularity(record);
                videoPopularity += dataTransform.ToCombinedVideoPopularity(record);

                Types.VTuberData vTuberData =
                    new(
                        id: record.Id,
                        activity: CommonActivityToJsonActivity(record.Activity),
                        name: record.DisplayName,
                        imgUrl: record.ImageUrl,
                        YouTube: dataTransform.ToYouTubeData(record.YouTube),
                        Twitch: dataTransform.ToTwitchData(record.Twitch),
                        popularVideo: dataTransform.GetPopularVideo(record),
                        group: record.GroupName,
                        nationality: record.Nationality,
                        debutDate: record.DebutDate?.ToString(Constant.DATE_FORMAT)
                    );

                lstMembers.Add(vTuberData);
            }

            if (lstMembers.Count == 0)
                continue;

            GroupData groupData =
                new(
                    id: groupName,
                    name: groupName,
                    popularity: totalPopularity,
                    livestreamPopularity: livestreamPopularity,
                    videoPopularity: videoPopularity,
                    members: lstMembers
                );

            rLst.Add(groupData);
        }

        return rLst;
    }

    public Dictionary<string, List<Types.VTuberData>> GroupMembers()
    {
        Dictionary<string, List<Types.VTuberData>> rDict = [];

        foreach (string groupName in _trackList.GetGroupNameList())
        {
            List<Types.VTuberData> lstMembers = [];

            foreach (
                KeyValuePair<VTuberId, VTuberRecord> vtuberStatPair in DictRecord
                    .Where(p => p.Value.Nationality.Contains(NationalityFilter))
                    .Where(p => p.Value.GroupName == groupName)
                    .OrderByDescending(
                        p => p,
                        new VTuberRecordComparator.CombinedCount(LatestBasicDataTime)
                    )
            )
            {
                VTuberRecord record = vtuberStatPair.Value;

                Types.VTuberData vTuberData =
                    new(
                        id: record.Id,
                        activity: CommonActivityToJsonActivity(record.Activity),
                        name: record.DisplayName == groupName
                            ? record.DisplayName + "(官方頻道)"
                            : record.DisplayName,
                        imgUrl: record.ImageUrl,
                        YouTube: dataTransform.ToYouTubeData(record.YouTube),
                        Twitch: dataTransform.ToTwitchData(record.Twitch),
                        popularVideo: dataTransform.GetPopularVideo(record),
                        group: record.GroupName,
                        nationality: record.Nationality,
                        debutDate: record.DebutDate?.ToString(Constant.DATE_FORMAT)
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

    private static GrowthRecordType GetGrowthResultToString(DictionaryRecord.GrowthType growthType)
    {
        return growthType switch
        {
            DictionaryRecord.GrowthType.Found => GrowthRecordType.full,
            DictionaryRecord.GrowthType.NotExact => GrowthRecordType.partial,
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
        return growthData.subscriber.count.HasValue
            ? ((growthData._7DaysGrowth.diff) / growthData.subscriber.count.Value)
            : decimal.MinValue;
    }
}
