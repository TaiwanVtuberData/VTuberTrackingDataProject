using Common.Types;
using Common.Types.Basic;
using Common.Utils;
using GenerateRecordList;
using GenerateRecordList.Types;
using GenerateRecordList.Utils;
using GenerateReportForPTT;

string dataRepoPath = args.Length >= 1 ? args[0] : "/tw_vtuber";

(_, DateTime latestRecordTime) = FileUtility.GetLatestRecord(dataRepoPath, "record");

DictionaryRecordToRecordList todayTransformer = GetDictionaryRecordToRecordList(
    dataRepoPath: dataRepoPath, target: latestRecordTime, timeSpan: new TimeSpan(days: 0, hours: 0, minutes: 0, seconds: 0));

DictionaryRecordToRecordList lastWeekTransformer = GetDictionaryRecordToRecordList(
    dataRepoPath: dataRepoPath, target: latestRecordTime, timeSpan: new TimeSpan(days: 7, hours: 0, minutes: 0, seconds: 0));

DictionaryRecordToRecordList lastMonthTransformer = GetDictionaryRecordToRecordList(
    dataRepoPath: dataRepoPath, target: latestRecordTime, timeSpan: new TimeSpan(days: 30, hours: 0, minutes: 0, seconds: 0));

WriteTrendingVTubers(todayTransformer, lastWeekTransformer, lastMonthTransformer);
WriteYouTubeSubscriberCountToPopularity(todayTransformer, lastWeekTransformer, lastMonthTransformer);
WriteYouTubeSubscriberCount(todayTransformer);
WriteYouTubeSubscriberGrowth(todayTransformer);

static DictionaryRecordToRecordList GetDictionaryRecordToRecordList(string dataRepoPath, DateTime target, TimeSpan timeSpan) {
    (_, DateTime latestRecordTime) = FileUtility.GetRecordAndDateDifference(dataRepoPath, "record", target, timeSpan);

    List<VTuberId> excluedList = FileUtility.GetListFromCsv(Path.Combine(dataRepoPath, "DATA/EXCLUDE_LIST.csv"));
    TrackList trackList = new(Path.Combine(dataRepoPath, "DATA/TW_VTUBER_TRACK_LIST.csv"), lstExcludeId: excluedList, throwOnValidationFail: true);

    (string latestBasicDataFilePath, DateTime latestBasicDataTime) = FileUtility.GetRecordAndDateDifference(dataRepoPath, "basic-data", target, timeSpan);
    Dictionary<VTuberId, VTuberBasicData> dictBasicData = VTuberBasicData.ReadFromCsv(latestBasicDataFilePath);


    if (latestBasicDataTime < latestRecordTime) {
        latestBasicDataTime = latestRecordTime;
    }

    DictionaryRecord dictRecord = new(trackList, excluedList, dictBasicData);
    MiscUtils.FillRecord(ref dictRecord, trackList: trackList, recordDir: dataRepoPath, targetDate: latestBasicDataTime.Date, recentDays: 35 - timeSpan.Days);
    MiscUtils.FillBasicData(ref dictRecord, trackList: trackList, basicDataDir: dataRepoPath, targetDate: latestBasicDataTime.Date, recentDays: 35 - timeSpan.Days);

    return new(
        trackList: trackList,
        dictRecord: dictRecord,
        todayDate: latestBasicDataTime.Date,
        latestRecordTime: latestRecordTime,
        latestBasicDataTime: latestBasicDataTime,
        nationalityFilter: "");
}

static void WriteTrendingVTubers(
    DictionaryRecordToRecordList todayTransformer,
    DictionaryRecordToRecordList lastWeekTransformer,
    DictionaryRecordToRecordList lastMonthTransformer
    ) {
    foreach (TrendingVTuberSortOrder sortOrder
    in new List<TrendingVTuberSortOrder>
    { TrendingVTuberSortOrder.livestream,
        TrendingVTuberSortOrder.video,
        TrendingVTuberSortOrder.combined }
    ) {
        List<VTuberPopularityData> todayVTuberList = todayTransformer.TrendingVTubers(sortBy: sortOrder, null);
        List<VTuberPopularityData> lastWeekVTuberList = lastWeekTransformer.TrendingVTubers(sortBy: sortOrder, null);
        List<VTuberPopularityData> lastMonthVTuberList = lastMonthTransformer.TrendingVTubers(sortBy: sortOrder, null);

        ChannelTable channelTable = new(valueHeader: "觀看中位數",
            sortByIncreasePercentage: false,
            onlyShowValueChanges: false);

        foreach (VTuberPopularityData vtuber in todayVTuberList) {
            VTuberPopularityData? lastWeekVTuber = lastWeekVTuberList.FirstOrDefault(e => e.id == vtuber.id);
            VTuberPopularityData? lastMonthVTuber = lastMonthVTuberList.FirstOrDefault(e => e.id == vtuber.id);

            decimal todayValue = (vtuber?.YouTube?.popularity ?? 0) + (vtuber?.Twitch?.popularity ?? 0);
            decimal lastWeekValue = (lastWeekVTuber?.YouTube?.popularity ?? 0) + (lastWeekVTuber?.Twitch?.popularity ?? 0);
            decimal lastMontuValue = (lastMonthVTuber?.YouTube?.popularity ?? 0) + (lastMonthVTuber?.Twitch?.popularity ?? 0);
            channelTable.AddChannel(
                channelName: vtuber?.name ?? "",
                currentValue: todayValue,
                lastWeekValue: lastWeekValue == 0m ? null : lastWeekValue,
                lastMonthValue: lastMontuValue == 0m ? null : lastMontuValue,
                isLesserThanLastWeek: lastWeekValue == 0m);
        }

        StreamWriter writer = new($"trending_{sortOrder}.txt");
        writer.Write(channelTable.ToString(maxColumnLength: 11));
        writer.Close();
    }
}


static void WriteYouTubeSubscriberCountToPopularity(
    DictionaryRecordToRecordList todayTransformer,
    DictionaryRecordToRecordList lastWeekTransformer,
    DictionaryRecordToRecordList lastMonthTransformer
    ) {
    foreach (TrendingVTuberSortOrder sortOrder
in new List<TrendingVTuberSortOrder>
{ TrendingVTuberSortOrder.livestream,
        TrendingVTuberSortOrder.video,
        TrendingVTuberSortOrder.combined }
) {
        List<VTuberSubscriberCountToPopularityData> todayVTuberList = todayTransformer.YouTubeSubscriberCountToPopularity(sortBy: sortOrder, count: null);
        List<VTuberSubscriberCountToPopularityData> lastWeekVTuberList = lastWeekTransformer.YouTubeSubscriberCountToPopularity(sortBy: sortOrder, null);
        List<VTuberSubscriberCountToPopularityData> lastMonthVTuberList = lastMonthTransformer.YouTubeSubscriberCountToPopularity(sortBy: sortOrder, null);

        ChannelTable channelTable = new(valueHeader: "訂閱者黏著度",
            sortByIncreasePercentage: false,
            onlyShowValueChanges: false);

        foreach (VTuberSubscriberCountToPopularityData vtuber in todayVTuberList) {
            VTuberSubscriberCountToPopularityData? lastWeekVTuber = lastWeekVTuberList.FirstOrDefault(e => e.id == vtuber.id);
            VTuberSubscriberCountToPopularityData? lastMonthVTuber = lastMonthVTuberList.FirstOrDefault(e => e.id == vtuber.id);

            decimal todayValue = (vtuber?.YouTube?.popularity ?? 0);
            decimal lastWeekValue = (lastWeekVTuber?.YouTube?.popularity ?? 0);
            decimal lastMontuValue = (lastMonthVTuber?.YouTube?.popularity ?? 0);
            channelTable.AddChannel(
                channelName: vtuber?.name ?? "",
                currentValue: todayValue,
                lastWeekValue: lastWeekValue == 0m ? null : lastWeekValue,
                lastMonthValue: lastMontuValue == 0m ? null : lastMontuValue,
                isLesserThanLastWeek: lastWeekValue == 0m);
        }

        StreamWriter writer = new($"subscriber_count_to_trending_{sortOrder}.txt");
        writer.Write(channelTable.ToString(maxColumnLength: 9));
        writer.Close();
    }
}

static void WriteYouTubeSubscriberCount(
    DictionaryRecordToRecordList todayTransformer
    ) {
    List<VTuberGrowthData> todayVTuberList = todayTransformer.GrowingVTubers(count: null, growthLimit: int.MinValue);

    ChannelTable channelTable = new(valueHeader: "訂閱人數",
        sortByIncreasePercentage: false,
        onlyShowValueChanges: false);

    foreach (VTuberGrowthData vtuber in todayVTuberList
        .OrderByDescending(e => e?.YouTube?.subscriber.count)
        ) {
        decimal todayValue = vtuber?.YouTube?.subscriber.count ?? 0m;
        decimal? lastWeekValue =
            vtuber?.YouTube?._7DaysGrowth.recordType != GrowthRecordType.full ? null : (todayValue - (vtuber?.YouTube?._7DaysGrowth.diff ?? 0m));
        decimal? lastMontuValue =
            vtuber?.YouTube?._30DaysGrowth.recordType != GrowthRecordType.full ? null : (todayValue - (vtuber?.YouTube?._30DaysGrowth.diff ?? 0m));

        channelTable.AddChannel(
            channelName: vtuber?.name ?? "",
            currentValue: todayValue,
            lastWeekValue: lastWeekValue,
            lastMonthValue: lastMontuValue,
            isLesserThanLastWeek: vtuber?.YouTube?._7DaysGrowth.recordType != GrowthRecordType.full);
    }

    StreamWriter writer = new($"subscriber_count.txt");
    writer.Write(channelTable.ToString(maxColumnLength: 13));
    writer.Close();
}

static void WriteYouTubeSubscriberGrowth(
    DictionaryRecordToRecordList todayTransformer
    ) {
    List<VTuberGrowthData> todayVTuberList = todayTransformer.GrowingVTubers(count: null);

    ChannelTable channelTable = new(valueHeader: "(no_header)",
        sortByIncreasePercentage: true,
        onlyShowValueChanges: false);

    foreach (VTuberGrowthData vtuber in todayVTuberList
        .OrderByDescending(e => e?.YouTube?.subscriber.count)
        ) {
        decimal todayValue = vtuber?.YouTube?.subscriber.count ?? 0m;
        decimal? lastWeekValue =
            vtuber?.YouTube?._7DaysGrowth.recordType != GrowthRecordType.full ? null : (todayValue - (vtuber?.YouTube?._7DaysGrowth.diff ?? 0m));
        decimal? lastMontuValue =
            vtuber?.YouTube?._30DaysGrowth.recordType != GrowthRecordType.full ? null : (todayValue - (vtuber?.YouTube?._30DaysGrowth.diff ?? 0m));

        channelTable.AddChannel(
            channelName: vtuber?.name ?? "",
            currentValue: todayValue,
            lastWeekValue: lastWeekValue,
            lastMonthValue: lastMontuValue,
            isLesserThanLastWeek: vtuber?.YouTube?._7DaysGrowth.recordType != GrowthRecordType.full);
    }

    StreamWriter writer = new($"subscriber_count_growth.txt");
    writer.Write(channelTable.ToString(maxColumnLength: 13));
    writer.Close();
}
