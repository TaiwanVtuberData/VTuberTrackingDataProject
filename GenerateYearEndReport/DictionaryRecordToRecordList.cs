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

    public List<YearEndVTuberGrowthData> GrowingVTubers(int? count)
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

            DictionaryRecord.GrowthResult _365DaysResult =
                DictRecord.GetYouTubeSubscriberCountGrowth(id, days: 365, daysLimit: 1);

            YearEndYouTubeGrowthData growthData =
                new(
                    id: record.YouTube.ChannelId,
                    subscriber: dataTransform.ToYouTubeSubscriber(record.YouTube),
                    _365DaysGrowth: new GrowthData(
                        diff: _365DaysResult.Growth,
                        recordType: GetGrowthResultToString(_365DaysResult.GrowthType)
                    ),
                    Nationality: record.Nationality
                );

            dictGrowth.Add(id, growthData);
        }

        List<YearEndVTuberGrowthData> rLst = [];

        foreach (
            KeyValuePair<VTuberId, YearEndYouTubeGrowthData> growthPair in dictGrowth
                .Where(p =>
                    p.Value.Nationality != null && p.Value.Nationality.Contains(NationalityFilter)
                )
                .Where(p => p.Value.subscriber.tag == CountTag.has)
                .Where(p => DictRecord[p.Key].YouTube != null)
                .OrderByDescending(p => p.Value._365DaysGrowth.diff)
                .Take(count ?? int.MaxValue)
        )
        {
            VTuberId id = growthPair.Key;
            YearEndYouTubeGrowthData youTubeGrowthData = growthPair.Value;

            VTuberRecord record = DictRecord[id];

            YearEndVTuberGrowthData vTuberData =
                new(
                    id: record.Id,
                    activity: CommonActivityToJsonActivity(record.Activity),
                    name: record.DisplayName,
                    imgUrl: record.ImageUrl,
                    YouTube: new YearEndYouTubeGrowthData(
                        id: youTubeGrowthData.id,
                        subscriber: youTubeGrowthData.subscriber,
                        _365DaysGrowth: youTubeGrowthData._365DaysGrowth,
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
