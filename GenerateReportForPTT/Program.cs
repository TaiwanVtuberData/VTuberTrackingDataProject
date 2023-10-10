using Common.Types;
using Common.Types.Basic;
using Common.Utils;
using GenerateRecordList;
using GenerateRecordList.Types;
using GenerateRecordList.Utils;
using GenerateReportForPTT;

string dataRepoPath = args.Length >= 1 ? args[0] : "/tw_vtuber";

(_, DateTime latestRecordTime) = FileUtility.GetLatestRecord(dataRepoPath, "record");

TrackList trackList = GetTrackList(dataRepoPath);

DictionaryRecordToRecordList todayTransformer = GetDictionaryRecordToRecordList(
    dataRepoPath: dataRepoPath, target: latestRecordTime, timeSpan: new TimeSpan(days: 0, hours: 0, minutes: 0, seconds: 0));

DictionaryRecordToRecordList lastWeekTransformer = GetDictionaryRecordToRecordList(
    dataRepoPath: dataRepoPath, target: latestRecordTime, timeSpan: new TimeSpan(days: 7, hours: 0, minutes: 0, seconds: 0));

DictionaryRecordToRecordList lastMonthTransformer = GetDictionaryRecordToRecordList(
    dataRepoPath: dataRepoPath, target: latestRecordTime, timeSpan: new TimeSpan(days: 30, hours: 0, minutes: 0, seconds: 0));

WriteTrendingVTubers(trackList.GetList(), todayTransformer, lastWeekTransformer, lastMonthTransformer);
WriteYouTubeSubscriberCountToPopularity(trackList.GetList(), todayTransformer, lastWeekTransformer, lastMonthTransformer);
WriteYouTubeSubscriberCount(trackList.GetList(), todayTransformer);
WriteYouTubeSubscriberGrowth(trackList.GetList(), todayTransformer);

static TrackList GetTrackList(string dataRepoPath) {
    List<VTuberId> excluedList = FileUtility.GetListFromCsv(Path.Combine(dataRepoPath, "DATA/EXCLUDE_LIST.csv"));
    TrackList trackList = new(Path.Combine(dataRepoPath, "DATA/TW_VTUBER_TRACK_LIST.csv"), lstExcludeId: excluedList, throwOnValidationFail: true);

    return trackList;
}

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
    List<Common.Types.VTuberData> vtuberDataList,
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
            Common.Types.VTuberData? vtuberData = vtuberDataList.FirstOrDefault(e => e.Id == vtuber.id);

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
                isLesserThanLastWeek: lastWeekValue == 0m,
                remarkText: GetRemarkText(vtuberData.Value.Activity, vtuberData.Value.DebuteDate, vtuberData.Value.GraduationDate, DateOnly.FromDateTime(DateTime.Today))
                );
        }

        StreamWriter writer = new($"trending_{sortOrder}.txt");
        writer.Write(channelTable.ToString(maxColumnLength: 11));
        writer.Close();
    }
}


static void WriteYouTubeSubscriberCountToPopularity(
    List<Common.Types.VTuberData> vtuberDataList,
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
            Common.Types.VTuberData? vtuberData = vtuberDataList.FirstOrDefault(e => e.Id == vtuber.id);

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
                isLesserThanLastWeek: lastWeekValue == 0m,
                remarkText: GetRemarkText(vtuberData.Value.Activity, vtuberData.Value.DebuteDate, vtuberData.Value.GraduationDate, DateOnly.FromDateTime(DateTime.Today))
                );
        }

        StreamWriter writer = new($"subscriber_count_to_trending_{sortOrder}.txt");
        writer.Write(channelTable.ToString(maxColumnLength: 9));
        writer.Close();
    }
}

static void WriteYouTubeSubscriberCount(
    List<Common.Types.VTuberData> vtuberDataList,
    DictionaryRecordToRecordList todayTransformer
    ) {
    List<VTuberGrowthData> todayVTuberList = todayTransformer.GrowingVTubers(count: null, growthLimit: int.MinValue);

    ChannelTable channelTable = new(valueHeader: "訂閱人數",
        sortByIncreasePercentage: false,
        onlyShowValueChanges: false);

    foreach (VTuberGrowthData vtuber in todayVTuberList
        .OrderByDescending(e => e?.YouTube?.subscriber.count)
        ) {
        Common.Types.VTuberData? vtuberData = vtuberDataList.FirstOrDefault(e => e.Id == vtuber.id);

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
            isLesserThanLastWeek: vtuber?.YouTube?._7DaysGrowth.recordType != GrowthRecordType.full,
            remarkText: GetRemarkText(vtuberData.Value.Activity, vtuberData.Value.DebuteDate, vtuberData.Value.GraduationDate, DateOnly.FromDateTime(DateTime.Today))
            );
    }

    StreamWriter writer = new($"subscriber_count.txt");
    writer.Write(channelTable.ToString(maxColumnLength: 13));
    writer.Close();
}

static void WriteYouTubeSubscriberGrowth(
    List<Common.Types.VTuberData> vtuberDataList,
    DictionaryRecordToRecordList todayTransformer
    ) {
    List<VTuberGrowthData> todayVTuberList = todayTransformer.GrowingVTubers(count: null);

    ChannelTable channelTable = new(valueHeader: "訂閱人數",
        sortByIncreasePercentage: true,
        onlyShowValueChanges: false);

    foreach (VTuberGrowthData vtuber in todayVTuberList
        .OrderByDescending(e => e?.YouTube?.subscriber.count)
        ) {
        Common.Types.VTuberData? vtuberData = vtuberDataList.FirstOrDefault(e => e.Id == vtuber.id);

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
            isLesserThanLastWeek: vtuber?.YouTube?._7DaysGrowth.recordType != GrowthRecordType.full,
            remarkText: GetRemarkText(vtuberData.Value.Activity, vtuberData.Value.DebuteDate, vtuberData.Value.GraduationDate, DateOnly.FromDateTime(DateTime.Today))
            );
    }

    StreamWriter writer = new($"subscriber_count_growth.txt");
    writer.Write(channelTable.ToString(maxColumnLength: 13));
    writer.Close();
}

static string GetRemarkText(Common.Types.Activity activity, DateOnly? debutDate, DateOnly? graduationDate, DateOnly todayDate) {
    if (graduationDate != null) {
        int graduationDateDifference = GetDateDifference(todayDate, graduationDate.Value);

        if (todayDate <= graduationDate && graduationDateDifference <= 30) {
            return $"將於 {graduationDate.Value:yyyy/MM/dd} 停止活動";
        } else {
            return "已停止活動";
        }
    }

    if (debutDate != null) {
        int debutDateDifference = GetDateDifference(todayDate, debutDate.Value);

        if (debutDateDifference <= 30) {
            if (todayDate <= debutDate) {
                return $"將於 {debutDate.Value:yyyy/MM/dd} 首次直播";
            } else {
                return $"{GetPaddedDateDifference(todayDate, debutDate.Value)} 天前首次直播";
            }
        }
    }

    if (activity == Common.Types.Activity.Graduated) {
        return "已停止活動";
    }

    if (activity == Common.Types.Activity.Preparing) {
        return "出道準備中";
    }

    return "";
}

static string GetPaddedDateDifference(DateOnly targetDate, DateOnly diffDate) {
    int dateDifference = GetDateDifference(targetDate, diffDate);

    if (dateDifference <= 9) {
        return $" {dateDifference}";
    } else {
        return $"{dateDifference}";
    }
}

static int GetDateDifference(DateOnly targetDate, DateOnly diffDate) {
    return (int)(
        targetDate.ToDateTime(TimeOnly.MinValue)
        - diffDate.ToDateTime(TimeOnly.MinValue)
        ).TotalDays;
}