using Common.Types;
using Common.Utils;
using GenerateJsonFile.Types;
using Microsoft.VisualBasic.FileIO;
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
        OUTPUT_PATH = args.Length >= 3 ? args[2] : "/out/api/v0";

        DateTime now = DateTime.UtcNow;

        (_, DateTime latestRecordTime) = FileUtility.GetLatestRecord(dataRepoPath, "record");
        TrackList trackList = new(Path.Combine(dataRepoPath, "DATA/TW_VTUBER_TRACK_LIST.csv"), requiredLevel: 999, throwOnValidationFail: true);
        List<string> excluedList = FileUtility.GetListFromCsv(Path.Combine(dataRepoPath, "DATA/EXCLUDE_LIST.csv"));

        (string latestBasicDataFilePath, _) = FileUtility.GetLatestRecord(dataRepoPath, "basic-data");
        Dictionary<string, VTuberBasicData> dictBasicData = VTuberBasicData.ReadFromCsv(latestBasicDataFilePath);

        DictionaryRecord dictRecord = new(trackList, excluedList, dictBasicData);
        FillRecord(ref dictRecord, trackList: trackList, recordDir: dataRepoPath, recentDays: 35);

        // Start output data
        ClearAndCreateOutputFolders();

        UpdateTimeWrapper updateTimeWrapper = new()
        {
            time = new UpdateTime()
            {
                statisticUpdateTime = latestRecordTime.ToUniversalTime().ToString("o"),
                VTuberDataUpdateTime = now.ToUniversalTime().ToString("o"),
            },
        };
        WriteJson(updateTimeWrapper, "update-time.json");

        List<VTuberFullData> lstAllVTuber = (new DictionaryRecordToJsonStruct(DateTime.Today, "")).AllWithFullData(dictRecord, latestRecordTime);
        foreach (VTuberFullData vtuber in lstAllVTuber)
        {
            WriteJson(vtuber, $"vtubers/{vtuber.id}.json");
        }

        foreach (var nationality in new List<(string, string)> { ("", "all"), ("TW", "TW"), ("HK", "HK"), ("MY", "MY") })
        {
            DictionaryRecordToJsonStruct transformer = new(DateTime.Today, nationality.Item1);

            foreach (var tuple in new List<(int?, string)> { (10, "10"), (null, "all") })
            {
                WriteJson(
                    transformer.All(dictRecord, latestRecordTime, tuple.Item1),
                    nationality.Item2,
                    $"vtubers/{tuple.Item2}.json");
            }

            foreach (var tuple in new List<(int?, string)> { (10, "10"), (100, "100") })
            {
                WriteJson(
                    transformer.TrendingVTubers(dictRecord, latestRecordTime, tuple.Item1),
                    nationality.Item2,
                    $"trending-vtubers/{tuple.Item2}.json");
            }

            foreach (var sortTuple in new List<(SortBy, string)> { (SortBy._7Days, "7-days"), (SortBy._30Days, "30-days") })
            {
                foreach (var countTuple in new List<(int?, string)> { (10, "10"), (100, "100"), (null, "all") })
                {
                    WriteJson(
                        transformer.VTubersViewCountChange(dictRecord, latestRecordTime, sortTuple.Item1, countTuple.Item1),
                    nationality.Item2,
                        $"vtubers-view-count-change/{sortTuple.Item2}/{countTuple.Item2}.json");
                }
            }

            foreach (var tuple in new List<(int?, string)> { (10, "10"), (100, "100"), (null, "all") })
            {
                WriteJson(
                    transformer.GrowingVTubers(dictRecord, latestRecordTime, tuple.Item1),
                    nationality.Item2,
                    $"growing-vtubers/{tuple.Item2}.json");
            }

            foreach (var tuple in new List<(uint, uint, string)> { (0, 7, "next-7-days"), (30, 30, "recent") })
            {
                WriteJson(
                    transformer.DebutVTubers(dictRecord, latestRecordTime, daysBefore: tuple.Item1, daysAfter: tuple.Item2),
                    nationality.Item2,
                    $"debut-vtubers/{tuple.Item3}.json");
            }

            foreach (var tuple in new List<(uint, uint, string)> { (0, 7, "next-7-days"), (30, 30, "recent") })
            {
                WriteJson(
                    transformer.GraduateVTubers(dictRecord, latestRecordTime, daysBefore: tuple.Item1, daysAfter: tuple.Item2),
                    nationality.Item2,
                    $"graduate-vtubers/{tuple.Item3}.json");
            }

            List<GroupData> lstGroupData = transformer.Groups(trackList, dictRecord, latestRecordTime);
            WriteJson(lstGroupData,
                    nationality.Item2,
                    "groups.json");

            Dictionary<string, List<Types.VTuberData>> dictGroupVTuberData = transformer.GroupMembers(trackList, dictRecord, latestRecordTime);
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

    readonly record struct UpdateTime(string statisticUpdateTime, string VTuberDataUpdateTime);
    readonly record struct UpdateTimeWrapper(UpdateTime time);
    readonly record struct VTuberFullWrapper(VTuberFullData VTuber);
    readonly record struct VTubersWrapper(List<Types.VTuberData> VTubers);
    readonly record struct VTubersGrowingWrapper(List<VTuberGrowthData> VTubers);
    readonly record struct VTubersDebutWrapper(List<VTuberDebutData> VTubers);
    readonly record struct VTubersGraduateWrapper(List<VTuberGraduateData> VTubers);
    readonly record struct VTubersPopularityWrapper(List<VTuberPopularityData> VTubers);
    readonly record struct VideosPopularityWrapper(List<VideoPopularityData> videos);
    readonly record struct GroupsWrapper(List<GroupData> groups);

    private static void WriteJson(UpdateTimeWrapper updateTimeWrapper, string outputFilePath)
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
            GetJsonString(new VTuberFullWrapper(VTuber: vTuberFullData)),
            "",
            outputFilePath
           );
    }

    private static void WriteJson(List<Types.VTuberData> lstVTuberData, string nationality, string outputFilePath)
    {
        WriteJsonString(
            GetJsonString(new VTubersWrapper(VTubers: lstVTuberData)),
            nationality,
            outputFilePath);
    }

    private static void WriteJson(List<VTuberGrowthData> lstVTuberData, string nationality, string outputFilePath)
    {
        WriteJsonString(
            GetJsonString(new VTubersGrowingWrapper(VTubers: lstVTuberData)),
            nationality,
            outputFilePath);
    }

    private static void WriteJson(List<VideoPopularityData> lstVideoData, string nationality, string outputFilePath)
    {
        WriteJsonString(
            GetJsonString(new VideosPopularityWrapper(videos: lstVideoData)),
            nationality,
            outputFilePath);
    }

    private static void WriteJson(List<VTuberDebutData> lstVTuberData, string nationality, string outputFilePath)
    {
        WriteJsonString(
            GetJsonString(new VTubersDebutWrapper(VTubers: lstVTuberData)),
            nationality,
            outputFilePath);
    }

    private static void WriteJson(List<VTuberGraduateData> lstVTuberData, string nationality, string outputFilePath)
    {
        WriteJsonString(
            GetJsonString(new VTubersGraduateWrapper(VTubers: lstVTuberData)),
            nationality,
            outputFilePath);
    }

    private static void WriteJson(List<VTuberPopularityData> lstVTuberData, string nationality, string outputFilePath)
    {
        WriteJsonString(
            GetJsonString(new VTubersPopularityWrapper(VTubers: lstVTuberData)),
            nationality,
            outputFilePath);
    }

    private static void WriteJson(List<GroupData> lstGroupData, string nationality, string outputFilePath)
    {
        WriteJsonString(
            GetJsonString(new GroupsWrapper(groups: lstGroupData)),
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
        List<Tuple<FileInfo, DateTime>> csvFileList = FileUtility.GetFileInfoDateTimeList(recordDir, recentDays);

        foreach (Tuple<FileInfo, DateTime> fileInfoDateTime in csvFileList)
        {
            Dictionary<string, VTuberStatistics> dictStatistics = GetStatisticsDictionaryFromRecordCSV(trackList, fileInfoDateTime.Item1.FullName);

            if (dictStatistics.Count >= trackList.GetCount() * 0.5)
            {
                dictRecord.AppendStatistic(fileInfoDateTime.Item2, dictStatistics);
            }
        }
    }

    private static Dictionary<string, VTuberStatistics> GetStatisticsDictionaryFromRecordCSV(TrackList trackList, string filePath)
    {
        // CSV Format:
        // Name,SubscriberCount,ViewCount,MedianViewCount,HighestViewCount
        // 鳥羽樂奈,40600,1613960,9725,23248
        // 香草奈若,26900,1509583,15267,57825

        TextFieldParser reader = new(filePath)
        {
            HasFieldsEnclosedInQuotes = true,
            Delimiters = new string[] { "," },
            CommentTokens = new string[] { "#" },
            TrimWhiteSpace = false,
            TextFieldType = FieldType.Delimited,
        };

        // consume header
        string[]? headerBlock = reader.ReadFields();

        if (headerBlock is null)
            return new();

        VTuberStatistics.Version version = VTuberStatistics.GetVersionByHeaderLength(headerBlock.Length);
        if (version == VTuberStatistics.Version.Unknown)
            return new();

        Dictionary<string, VTuberStatistics> rDict = new();

        while (!reader.EndOfData)
        {
            string[]? entryBlock = reader.ReadFields();

            if (entryBlock is null || entryBlock.Length < 1)
            {
                continue;
            }

            string name = entryBlock[0];
            string displayName = trackList.GetDisplayName(name);

            rDict.Add(displayName, new VTuberStatistics(entryBlock));
        }

        return rDict;
    }
}
