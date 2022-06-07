using Common.Types;
using Common.Utils;
using GenerateJsonFile.Types;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace GenerateJsonFile;

class Program
{
    private static string OUTPUT_PATH = "";
    static void Main(string[] args)
    {
        string dataRepoPath = args.Length >= 1 ? args[0] : "/tw_vtuber";
        OUTPUT_PATH = args.Length >= 2 ? args[1] : "/out/api/v2";

        (_, DateTime latestRecordTime) = FileUtility.GetLatestRecord(dataRepoPath, "record");

        List<string> excluedList = FileUtility.GetListFromCsv(Path.Combine(dataRepoPath, "DATA/EXCLUDE_LIST.csv"));
        TrackList trackList = new(Path.Combine(dataRepoPath, "DATA/TW_VTUBER_TRACK_LIST.csv"), lstExcludeId: excluedList, throwOnValidationFail: true);

        (string latestBasicDataFilePath, DateTime latestBasicDataTime) = FileUtility.GetLatestRecord(dataRepoPath, "basic-data");
        Dictionary<string, VTuberBasicData> dictBasicData = VTuberBasicData.ReadFromCsv(latestBasicDataFilePath);

        (string latestLivestreamsFilePath, _) = FileUtility.GetLatestRecord(dataRepoPath, "livestreams");
        LiveVideosList liveVideos = new(latestLivestreamsFilePath, throwOnValidationFail: true);

        DictionaryRecord dictRecord = new(trackList, excluedList, dictBasicData);
        FillRecord(ref dictRecord, trackList: trackList, recordDir: dataRepoPath, recentDays: 35);
        FillBasicData(ref dictRecord, trackList: trackList, basicDataDir: dataRepoPath, recentDays: 35);

        // Start output data
        ClearAndCreateOutputFolders();

        if (latestBasicDataTime < latestRecordTime)
        {
            latestBasicDataTime = latestRecordTime;
        }

        UpdateTimeResponse updateTimeResponse = new()
        {
            time = new UpdateTime()
            {
                statisticUpdateTime = latestRecordTime.ToUniversalTime().ToString("o"),
                VTuberDataUpdateTime = latestBasicDataTime.ToUniversalTime().ToString("o"),
            },
        };
        WriteJson(updateTimeResponse, "update-time.json");

        List<VTuberFullData> lstAllVTuber = new DictionaryRecordToJsonStruct(trackList, dictRecord, DateTime.Today, latestRecordTime, latestBasicDataTime, "").AllWithFullData();
        foreach (VTuberFullData vtuber in lstAllVTuber)
        {
            WriteJson(vtuber, $"vtubers/{vtuber.id}.json");
        }

        foreach (var nationality in new List<(string, string)> { ("", "all"), ("TW", "TW"), ("HK", "HK"), ("MY", "MY") })
        {
            DictionaryRecordToJsonStruct transformer = new(trackList, dictRecord, DateTime.Today, latestRecordTime, latestBasicDataTime, nationality.Item1);

            foreach (var tuple in new List<(int?, string)> { (10, "10"), (null, "all") })
            {
                WriteJson(
                    transformer.All(count: tuple.Item1),
                    nationality.Item2,
                    $"vtubers/{tuple.Item2}.json");
            }

            foreach (var tuple in new List<(int?, string)> { (10, "10"), (100, "100") })
            {
                WriteJson(
                    transformer.TrendingVTubers(count: tuple.Item1),
                    nationality.Item2,
                    $"trending-vtubers/{tuple.Item2}.json");
            }

            foreach (var sortTuple in new List<(SortBy, string)> { (SortBy._7Days, "7-days"), (SortBy._30Days, "30-days") })
            {
                foreach (var countTuple in new List<(int?, string)> { (10, "10"), (100, "100"), (null, "all") })
                {
                    WriteJson(
                        transformer.VTubersViewCountChange(sortBy: sortTuple.Item1, count: countTuple.Item1),
                    nationality.Item2,
                        $"vtubers-view-count-change/{sortTuple.Item2}/{countTuple.Item2}.json");
                }
            }

            foreach (var tuple in new List<(int?, string)> { (10, "10"), (100, "100"), (null, "all") })
            {
                WriteJson(
                    transformer.GrowingVTubers(count: tuple.Item1),
                    nationality.Item2,
                    $"growing-vtubers/{tuple.Item2}.json");
            }

            foreach (var tuple in new List<(uint, uint, string)> { (0, 7, "next-7-days"), (30, 30, "recent") })
            {
                WriteJson(
                    transformer.DebutVTubers(daysBefore: tuple.Item1, daysAfter: tuple.Item2),
                    nationality.Item2,
                    $"debut-vtubers/{tuple.Item3}.json");
            }

            foreach (var tuple in new List<(uint, uint, string)> { (0, 7, "next-7-days"), (30, 30, "recent") })
            {
                WriteJson(
                    transformer.GraduateVTubers(daysBefore: tuple.Item1, daysAfter: tuple.Item2),
                    nationality.Item2,
                    $"graduate-vtubers/{tuple.Item3}.json");
            }

            List<GroupData> lstGroupData = transformer.Groups();
            WriteJson(lstGroupData,
                    nationality.Item2,
                    "groups.json");

            Dictionary<string, List<Types.VTuberData>> dictGroupVTuberData = transformer.GroupMembers();
            foreach (KeyValuePair<string, List<Types.VTuberData>> entry in dictGroupVTuberData)
            {
                string outputDir = $"groups/{entry.Key}";
                WriteJson(
                    entry.Value,
                    nationality.Item2,
                    $"{outputDir}/vtubers.json");
            }

            (string latestFilePath, _) = FileUtility.GetLatestRecord(dataRepoPath, "top-videos");
            TopVideosList topVideoList = FileUtility.GetTopVideoList(latestFilePath);

            TopVideosListToJsonStruct videoTransformer = new(nationality.Item1);

            WriteJson(
                videoTransformer.Get(trackList, topVideoList, dictRecord, 100, allowDuplicate: true),
                nationality.Item2,
                "trending-videos/all.json");

            WriteJson(
                videoTransformer.Get(trackList, topVideoList, dictRecord, 100, allowDuplicate: false),
                nationality.Item2,
                "trending-videos/no-duplicate.json");
        }
    }

    private static void ClearAndCreateOutputFolders()
    {
        ClearAndCreateOutputFolder(Path.Combine(OUTPUT_PATH));
    }

    private static void ClearAndCreateOutputFolder(string outputFolder)
    {
        if (Directory.Exists(outputFolder))
        {
            Directory.Delete(outputFolder, true);
        }
        Directory.CreateDirectory(outputFolder);
    }


    private static void WriteJson(UpdateTimeResponse updateTimeWrapper, string outputFilePath)
    {
        WriteJsonString(
            GetJsonString(updateTimeWrapper),
            "",
            outputFilePath
           );
    }
    private static void WriteJson(VTuberFullData vTuberFullData, string outputFilePath)
    {
        WriteJsonString(
            GetJsonString(new SingleVTuberFullDataResponse(VTuber: vTuberFullData)),
            "",
            outputFilePath
           );
    }

    private static void WriteJson(List<Types.VTuberData> lstVTuberData, string nationality, string outputFilePath)
    {
        WriteJsonString(
            GetJsonString(new VTuberDataResponse(VTubers: lstVTuberData)),
            nationality,
            outputFilePath);
    }
    private static void WriteJson(List<VTuberViewCountGrowthData> lstVTuberData, string nationality, string outputFilePath)
    {
        WriteJsonString(
            GetJsonString(new VTuberViewCountChangeDataResponse(VTubers: lstVTuberData)),
            nationality,
            outputFilePath);
    }

    private static void WriteJson(List<VTuberGrowthData> lstVTuberData, string nationality, string outputFilePath)
    {
        WriteJsonString(
            GetJsonString(new VTuberGrowthDataResponse(VTubers: lstVTuberData)),
            nationality,
            outputFilePath);
    }

    private static void WriteJson(List<VideoPopularityData> lstVideoData, string nationality, string outputFilePath)
    {
        WriteJsonString(
            GetJsonString(new VideoPopularityDataResponse(videos: lstVideoData)),
            nationality,
            outputFilePath);
    }

    private static void WriteJson(List<VTuberDebutData> lstVTuberData, string nationality, string outputFilePath)
    {
        WriteJsonString(
            GetJsonString(new VTuberDebutDataResponse(VTubers: lstVTuberData)),
            nationality,
            outputFilePath);
    }

    private static void WriteJson(List<VTuberGraduateData> lstVTuberData, string nationality, string outputFilePath)
    {
        WriteJsonString(
            GetJsonString(new VTuberGraduateDataResponse(VTubers: lstVTuberData)),
            nationality,
            outputFilePath);
    }

    private static void WriteJson(List<VTuberPopularityData> lstVTuberData, string nationality, string outputFilePath)
    {
        WriteJsonString(
            GetJsonString(new VTuberPopularityDataResponse(VTubers: lstVTuberData)),
            nationality,
            outputFilePath);
    }

    private static void WriteJson(List<GroupData> lstGroupData, string nationality, string outputFilePath)
    {
        WriteJsonString(
            GetJsonString(new GroupDataResponse(groups: lstGroupData)),
            nationality,
            outputFilePath);
    }

    private static string GetJsonString(object obj)
    {
        JsonSerializerOptions options = new()
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        return JsonSerializer.Serialize(obj, options);
    }

    private static void WriteJsonString(string jsonString, string nationality, string outputFilePath)
    {
        string? outputFolder = Path.GetDirectoryName(Path.Combine(OUTPUT_PATH, nationality, outputFilePath));
        if (outputFolder is not null && !Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        StreamWriter writer = new(Path.Combine(OUTPUT_PATH, nationality, outputFilePath))
        {
            NewLine = "\n",
        };

        writer.Write(jsonString);
        writer.Close();
    }

    private static void FillRecord(ref DictionaryRecord dictRecord, TrackList trackList, string recordDir, int recentDays)
    {
        List<Tuple<FileInfo, DateTime>> csvFileList = FileUtility.GetFileInfoDateTimeList(
            directory: recordDir,
            prefix: "record",
            recentDays: recentDays);

        foreach (Tuple<FileInfo, DateTime> fileInfoDateTime in csvFileList)
        {
            Dictionary<string, VTuberStatistics> dictStatistics = CsvUtility.ReadStatisticsDictionary(trackList, fileInfoDateTime.Item1.FullName);

            if (dictStatistics.Count >= trackList.GetCount() * 0.5)
            {
                dictRecord.AppendStatistic(fileInfoDateTime.Item2, dictStatistics);
                dictRecord.AppendBasicData(fileInfoDateTime.Item2, dictStatistics);
            }
        }
    }

    private static void FillBasicData(ref DictionaryRecord dictRecord, TrackList trackList, string basicDataDir, int recentDays)
    {
        List<Tuple<FileInfo, DateTime>> csvFileList = FileUtility.GetFileInfoDateTimeList(
            directory: basicDataDir,
            prefix: "basic-data",
            recentDays: recentDays);

        foreach (Tuple<FileInfo, DateTime> fileInfoDateTime in csvFileList)
        {
            Dictionary<string, VTuberBasicData> dictBasicData = VTuberBasicData.ReadFromCsv(fileInfoDateTime.Item1.FullName);

            if (dictBasicData.Count >= trackList.GetCount() * 0.5)
            {
                dictRecord.AppendBasicData(fileInfoDateTime.Item2, dictBasicData);
            }
        }
    }
}
