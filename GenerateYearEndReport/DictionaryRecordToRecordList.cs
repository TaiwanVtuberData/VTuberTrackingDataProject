using Common.Types;
using Common.Types.Basic;
using GenerateRecordList.Types;
using GenerateYearEndReport.Types;
using GenerateYearEndReport.Utils;

namespace GenerateYearEndReport;

public class DictionaryRecordToRecordList(
    TrackList trackList,
    DictionaryRecord dictRecord,
    DateOnly todayDate,
    DateTimeOffset latestRecordTime,
    DateTimeOffset latestBasicDataTime,
    string nationalityFilter
)
{
    private readonly TrackList _trackList = trackList;
    private readonly DictionaryRecord DictRecord = dictRecord;
    private readonly DateOnly TodayDate = todayDate;
    private readonly DateTimeOffset LatestRecordTime = latestRecordTime;
    private readonly DateTimeOffset LatestBasicDataTime = latestBasicDataTime;
    private readonly DataTransform dataTransform = new(latestRecordTime, latestBasicDataTime);
    private readonly string NationalityFilter = nationalityFilter;

    public enum FilterOption
    {
        Before,
        AfterOrEqual,
    }

    public record GrowingVTubersFilterOption(FilterOption FilterOption, DateOnly DebutDate) { }

    public List<YearEndVTuberYouTubeGrowthData> YouTubeGrowingVTubers(
        int? count,
        int recentDays,
        GrowingVTubersFilterOption growingVTubersFilterOption
    )
    {
        Dictionary<VTuberId, YearEndYouTubeGrowthData> dictGrowth = new(DictRecord.Count);

        foreach (KeyValuePair<VTuberId, VTuberRecord> vtuberStatPair in DictRecord)
        {
            VTuberId id = vtuberStatPair.Key;
            VTuberRecord record = vtuberStatPair.Value;

            if (record.YouTube == null)
            {
                continue;
            }

            if (!MatchFilter(record.DebutDate, growingVTubersFilterOption))
            {
                continue;
            }

            DictionaryRecord.GrowthResult _1YearResult =
                DictRecord.GetYouTubeSubscriberCountGrowth(id, days: recentDays, daysLimit: 1);

            YearEndYouTubeGrowthData growthData =
                new(
                    id: record.YouTube.ChannelId,
                    subscriber: dataTransform.ToYouTubeSubscriber(record.YouTube),
                    _1YearGrowth: new GrowthData(
                        diff: _1YearResult.Growth,
                        recordType: GetGrowthResultToString(_1YearResult.GrowthType)
                    ),
                    Nationality: record.Nationality
                );

            dictGrowth.Add(id, growthData);
        }

        List<YearEndVTuberYouTubeGrowthData> rLst = [];

        foreach (
            KeyValuePair<VTuberId, YearEndYouTubeGrowthData> growthPair in dictGrowth
                .Where(p =>
                    p.Value.Nationality != null && p.Value.Nationality.Contains(NationalityFilter)
                )
                .Where(p => p.Value.subscriber.tag == CountTag.has)
                .Where(p => DictRecord[p.Key].YouTube != null)
                .Where(p => p.Value._1YearGrowth.diff > 0)
                .OrderByDescending(p => p.Value._1YearGrowth.diff)
                .Take(count ?? int.MaxValue)
        )
        {
            VTuberId id = growthPair.Key;
            YearEndYouTubeGrowthData youTubeGrowthData = growthPair.Value;

            VTuberRecord record = DictRecord[id];

            YearEndVTuberYouTubeGrowthData vTuberData =
                new(
                    id: record.Id,
                    activity: CommonActivityToJsonActivity(record.Activity),
                    name: record.DisplayName,
                    imgUrl: record.ImageUrl,
                    YouTube: new YearEndYouTubeGrowthData(
                        id: youTubeGrowthData.id,
                        subscriber: youTubeGrowthData.subscriber,
                        _1YearGrowth: youTubeGrowthData._1YearGrowth,
                        Nationality: null
                    ),
                    Twitch: dataTransform.ToTwitchData(record.Twitch),
                    group: record.GroupName,
                    nationality: record.Nationality,
                    debutDate: record.DebutDate?.ToString(Constant.DATE_FORMAT)
                );

            rLst.Add(vTuberData);
        }

        return rLst;
    }

    public List<YearEndVTuberTwitchGrowthData> TwitchGrowingVTubers(
        int? count,
        int recentDays,
        GrowingVTubersFilterOption growingVTubersFilterOption
    )
    {
        Dictionary<VTuberId, YearEndTwitchGrowthData> dictGrowth = new(DictRecord.Count);

        foreach (KeyValuePair<VTuberId, VTuberRecord> vtuberStatPair in DictRecord)
        {
            VTuberId id = vtuberStatPair.Key;
            VTuberRecord record = vtuberStatPair.Value;

            if (record.Twitch == null)
            {
                continue;
            }

            if (!MatchFilter(record.DebutDate, growingVTubersFilterOption))
            {
                continue;
            }

            DictionaryRecord.GrowthResult _1YearResult = DictRecord.GetTwitchFollowerCountGrowth(
                id,
                days: recentDays,
                daysLimit: 1
            );

            YearEndTwitchGrowthData growthData =
                new(
                    id: record.Twitch.ChannelId,
                    follower: dataTransform.ToTwitchFollower(record.Twitch),
                    _1YearGrowth: new GrowthData(
                        diff: _1YearResult.Growth,
                        recordType: GetGrowthResultToString(_1YearResult.GrowthType)
                    ),
                    Nationality: record.Nationality
                );

            dictGrowth.Add(id, growthData);
        }

        List<YearEndVTuberTwitchGrowthData> rLst = [];

        foreach (
            KeyValuePair<VTuberId, YearEndTwitchGrowthData> growthPair in dictGrowth
                .Where(p =>
                    p.Value.Nationality != null && p.Value.Nationality.Contains(NationalityFilter)
                )
                .Where(p => p.Value.follower.tag == CountTag.has)
                .Where(p => DictRecord[p.Key].YouTube != null)
                .Where(p => p.Value._1YearGrowth.diff > 0)
                .OrderByDescending(p => p.Value._1YearGrowth.diff)
                .Take(count ?? int.MaxValue)
        )
        {
            VTuberId id = growthPair.Key;
            YearEndTwitchGrowthData twitchGrowthData = growthPair.Value;

            VTuberRecord record = DictRecord[id];

            YearEndVTuberTwitchGrowthData vTuberData =
                new(
                    id: record.Id,
                    activity: CommonActivityToJsonActivity(record.Activity),
                    name: record.DisplayName,
                    imgUrl: record.ImageUrl,
                    YouTube: dataTransform.ToYouTubeData(record.YouTube),
                    Twitch: new YearEndTwitchGrowthData(
                        id: twitchGrowthData.id,
                        follower: twitchGrowthData.follower,
                        _1YearGrowth: twitchGrowthData._1YearGrowth,
                        Nationality: null
                    ),
                    group: record.GroupName,
                    nationality: record.Nationality,
                    debutDate: record.DebutDate?.ToString(Constant.DATE_FORMAT)
                );

            rLst.Add(vTuberData);
        }

        return rLst;
    }

    public List<YearEndVTuberYouTubeViewCountGrowthData> YouTubeVTubersViewCountChange(
        int? count,
        int recentDays,
        GrowingVTubersFilterOption growingVTubersFilterOption
    )
    {
        Dictionary<VTuberId, YearEndYouTubeViewCountGrowthData> dictGrowth = new(DictRecord.Count);

        foreach (KeyValuePair<VTuberId, VTuberRecord> vtuberStatPair in DictRecord)
        {
            VTuberId id = vtuberStatPair.Key;
            VTuberRecord record = vtuberStatPair.Value;

            if (record.YouTube == null)
            {
                continue;
            }

            if (!MatchFilter(record.DebutDate, growingVTubersFilterOption))
            {
                continue;
            }

            DictionaryRecord.GrowthResult _1YearResult = DictRecord.GetYouTubeViewCountGrowth(
                id,
                days: recentDays,
                daysLimit: 1
            );

            YearEndYouTubeViewCountGrowthData growthData =
                new(
                    id: record.YouTube.ChannelId,
                    totalViewCount: dataTransform.ToYouTubeTotalViewCount(record.YouTube),
                    _1YearGrowth: new GrowthData(
                        diff: _1YearResult.Growth,
                        recordType: GetGrowthResultToString(_1YearResult.GrowthType)
                    ),
                    Nationality: record.Nationality
                );

            dictGrowth.Add(id, growthData);
        }

        List<YearEndVTuberYouTubeViewCountGrowthData> rLst = [];

        foreach (
            KeyValuePair<VTuberId, YearEndYouTubeViewCountGrowthData> growthPair in dictGrowth
                .Where(p =>
                    p.Value.Nationality != null && p.Value.Nationality.Contains(NationalityFilter)
                )
                .Where(p => p.Value.totalViewCount != 0)
                .Where(p => p.Value._1YearGrowth.diff > 0)
                .Where(p => DictRecord[p.Key].YouTube != null)
                .OrderByDescending(p => p.Value._1YearGrowth.diff)
                .Take(count ?? int.MaxValue)
        )
        {
            VTuberId id = growthPair.Key;
            YearEndYouTubeViewCountGrowthData youTubeGrowthData = growthPair.Value;

            VTuberRecord record = DictRecord[id];

            YearEndVTuberYouTubeViewCountGrowthData vTuberData =
                new(
                    id: record.Id,
                    activity: CommonActivityToJsonActivity(record.Activity),
                    name: record.DisplayName,
                    imgUrl: record.ImageUrl,
                    YouTube: new YearEndYouTubeViewCountGrowthData(
                        id: youTubeGrowthData.id,
                        totalViewCount: youTubeGrowthData.totalViewCount,
                        _1YearGrowth: youTubeGrowthData._1YearGrowth,
                        Nationality: null
                    ),
                    Twitch: dataTransform.ToTwitchData(record.Twitch),
                    group: record.GroupName,
                    nationality: record.Nationality,
                    debutDate: record.DebutDate?.ToString(Constant.DATE_FORMAT)
                );

            rLst.Add(vTuberData);
        }

        return rLst;
    }

    private static bool MatchFilter(
        DateOnly? debutDate,
        GrowingVTubersFilterOption growingVTubersFilterOption
    )
    {
        // consider VTubers without debut date to be before any date
        if (debutDate == null)
        {
            switch (growingVTubersFilterOption.FilterOption)
            {
                case FilterOption.Before:
                    return true;
                case FilterOption.AfterOrEqual:
                    return false;
            }
        }

        switch (growingVTubersFilterOption.FilterOption)
        {
            case FilterOption.Before:
                return debutDate < growingVTubersFilterOption.DebutDate;
            case FilterOption.AfterOrEqual:
                return debutDate >= growingVTubersFilterOption.DebutDate;
        }

        return false;
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

    private static GenerateRecordList.Types.Activity CommonActivityToJsonActivity(
        Common.Types.Activity commonActivity
    )
    {
        return commonActivity switch
        {
            Common.Types.Activity.Preparing => GenerateRecordList.Types.Activity.preparing,
            Common.Types.Activity.Active => GenerateRecordList.Types.Activity.active,
            Common.Types.Activity.Graduated => GenerateRecordList.Types.Activity.graduate,
            _ => throw new InvalidOperationException(),
        };
    }
}
