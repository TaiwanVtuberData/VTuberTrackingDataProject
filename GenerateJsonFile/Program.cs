using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Common.Types;
using Common.Types.Basic;
using Common.Utils;
using GenerateJsonFile.Types;
using GenerateRecordList;
using GenerateRecordList.Types;
using GenerateRecordList.Utils;

namespace GenerateJsonFile;

class Program
{
    private static string OUTPUT_PATH = "";

    private static readonly JsonSerializerOptions jsonSerializerOptions =
        new()
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            WriteIndented = false,
            DefaultIgnoreCondition = System
                .Text
                .Json
                .Serialization
                .JsonIgnoreCondition
                .WhenWritingNull,
        };

    static void Main(string[] args)
    {
        string? DATA_REPO_PATH = Environment.GetEnvironmentVariable("DATA_REPO_PATH");
        Console.WriteLine($"DATA_REPO_PATH: {DATA_REPO_PATH}");

        string? OUTPUT_PATH_TEMP = Environment.GetEnvironmentVariable("OUTPUT_PATH");
        Console.WriteLine($"OUTPUT_PATH: {OUTPUT_PATH}");

        if (DATA_REPO_PATH == null || OUTPUT_PATH_TEMP == null)
        {
            Console.WriteLine(
                "Environment variables [DATA_REPO_PATH], and/or [OUTPUT_PATH] missing. Abort program."
            );
            return;
        }
        OUTPUT_PATH = OUTPUT_PATH_TEMP;

        DateTimeOffset now = DateTimeOffset.Now;

        (_, DateTimeOffset latestRecordTime) = FileUtility.GetLatestRecord(
            DATA_REPO_PATH,
            "record"
        );

        List<VTuberId> excludeList = FileUtility.GetListFromCsv(
            Path.Combine(DATA_REPO_PATH, "DATA/EXCLUDE_LIST.csv")
        );
        TrackList trackList =
            new(
                Path.Combine(DATA_REPO_PATH, "DATA/TW_VTUBER_TRACK_LIST.csv"),
                lstExcludeId: excludeList,
                ignoreGraduated: false,
                throwOnValidationFail: true
            );

        (string latestBasicDataFilePath, DateTimeOffset latestBasicDataTime) =
            FileUtility.GetLatestRecord(DATA_REPO_PATH, "basic-data");
        Dictionary<VTuberId, VTuberBasicData> dictBasicData = VTuberBasicData.ReadFromCsv(
            latestBasicDataFilePath
        );

        (string latestLivestreamsFilePath, DateTimeOffset latestLivestreamsDateTime) =
            FileUtility.GetLatestRecord(DATA_REPO_PATH, "livestreams");
        LiveVideosList liveVideos =
            new(latestLivestreamsFilePath, clearGarbage: true, throwOnValidationFail: true);

        (string latestTwitchLivestreamsFilePath, DateTimeOffset latestTwitchLivestreamsDateTime) =
            FileUtility.GetLatestRecord(DATA_REPO_PATH, "twitch-livestreams");
        LiveVideosList twitchLiveVideos =
            new(latestTwitchLivestreamsFilePath, clearGarbage: true, throwOnValidationFail: true);

        // if latestTwitchLivestreamsDateTime is after latestLivestreamsDateTime
        // then clear Twitch Livestreams in liveVideos and insert newest twitchLiveVideos
        if (latestTwitchLivestreamsDateTime > latestLivestreamsDateTime)
        {
            liveVideos = ClearTwitchLiveVideos(liveVideos).Insert(twitchLiveVideos);
        }

        if (latestBasicDataTime < latestRecordTime)
        {
            latestBasicDataTime = latestRecordTime;
        }

        DictionaryRecord dictRecord = new(trackList, excludeList, dictBasicData);
        MiscUtils.FillRecord(
            ref dictRecord,
            trackList: trackList,
            recordDir: DATA_REPO_PATH,
            targetDate: latestBasicDataTime.Date,
            recentDays: 35
        );
        MiscUtils.FillBasicData(
            ref dictRecord,
            trackList: trackList,
            basicDataDir: DATA_REPO_PATH,
            targetDate: latestBasicDataTime.Date,
            recentDays: 35
        );

        // Start output data
        ClearAndCreateOutputFolders();

        UpdateTimeResponse updateTimeResponse =
            new(
                time: new UpdateTime(
                    statisticUpdateTime: MiscUtils.ToIso8601UtcString(
                        latestTwitchLivestreamsDateTime
                    ),
                    VTuberDataUpdateTime: MiscUtils.ToIso8601UtcString(latestBasicDataTime)
                )
            );
        WriteJson(updateTimeResponse, "update-time.json");

        List<VTuberFullData> lstAllVTuber = new DictionaryRecordToRecordList(
            trackList,
            dictRecord,
            DateOnly.FromDateTime(DateTime.Today),
            latestRecordTime,
            latestBasicDataTime,
            ""
        ).AllWithFullData(liveVideos);
        foreach (VTuberFullData vtuber in lstAllVTuber)
        {
            WriteJson(vtuber, $"vtubers/{vtuber.id.Value}.json");
        }

        foreach (
            var nationality in new List<(string, string)>
            {
                ("", "all"),
                ("TW", "TW"),
                ("HK", "HK"),
                ("MY", "MY"),
            }
        )
        {
            DictionaryRecordToRecordList transformer =
                new(
                    trackList,
                    dictRecord,
                    DateOnly.FromDateTime(DateTime.Today),
                    latestRecordTime,
                    latestBasicDataTime,
                    nationality.Item1
                );

            foreach (var tuple in new List<(int?, string)> { (10, "10"), (null, "all") })
            {
                WriteJson(
                    transformer.All(count: tuple.Item1),
                    nationality.Item2,
                    $"vtubers/{tuple.Item2}.json"
                );
            }

            foreach (var sort in Enum.GetValues<TrendingVTuberSortOrder>())
            {
                foreach (var tuple in new List<(int?, string)> { (10, "10"), (100, "100") })
                {
                    WriteJson(
                        transformer.TrendingVTubers(sortBy: sort, count: tuple.Item1),
                        nationality.Item2,
                        $"trending-vtubers/{sort}/{tuple.Item2}.json"
                    );
                }
            }

            foreach (
                var sortTuple in new List<(SortBy, string)>
                {
                    (SortBy._7Days, "7-days"),
                    (SortBy._30Days, "30-days"),
                }
            )
            {
                foreach (
                    var countTuple in new List<(int?, string)>
                    {
                        (10, "10"),
                        (100, "100"),
                        (null, "all"),
                    }
                )
                {
                    WriteJson(
                        transformer.VTubersViewCountChange(
                            sortBy: sortTuple.Item1,
                            count: countTuple.Item1
                        ),
                        nationality.Item2,
                        $"vtubers-view-count-change/{sortTuple.Item2}/{countTuple.Item2}.json"
                    );
                }
            }

            foreach (
                var tuple in new List<(int?, string)> { (10, "10"), (100, "100"), (null, "all") }
            )
            {
                WriteJson(
                    transformer.GrowingVTubers(count: tuple.Item1),
                    nationality.Item2,
                    $"growing-vtubers/{tuple.Item2}.json"
                );
            }

            foreach (
                var tuple in new List<(uint, uint, string)>
                {
                    (0, 7, "next-7-days"),
                    (30, 30, "recent"),
                }
            )
            {
                WriteJson(
                    transformer.DebutVTubers(daysBefore: tuple.Item1, daysAfter: tuple.Item2),
                    nationality.Item2,
                    $"debut-vtubers/{tuple.Item3}.json"
                );
            }

            foreach (
                var tuple in new List<(uint, uint, string)>
                {
                    (0, 7, "next-7-days"),
                    (30, 30, "recent"),
                }
            )
            {
                WriteJson(
                    transformer.AnniversaryVTubers(daysBefore: tuple.Item1, daysAfter: tuple.Item2),
                    nationality.Item2,
                    $"anniversary-vtubers/{tuple.Item3}.json"
                );
            }

            foreach (
                var tuple in new List<(uint, uint, string)>
                {
                    (0, 7, "next-7-days"),
                    (30, 30, "recent"),
                }
            )
            {
                WriteJson(
                    transformer.GraduateVTubers(daysBefore: tuple.Item1, daysAfter: tuple.Item2),
                    nationality.Item2,
                    $"graduate-vtubers/{tuple.Item3}.json"
                );
            }

            List<GroupData> lstGroupData = transformer.Groups();
            WriteJson(lstGroupData, nationality.Item2, "groups.json");

            Dictionary<string, List<GenerateRecordList.Types.VTuberData>> dictGroupVTuberData =
                transformer.GroupMembers();
            foreach (
                KeyValuePair<
                    string,
                    List<GenerateRecordList.Types.VTuberData>
                > entry in dictGroupVTuberData
            )
            {
                string outputDir = $"groups/{entry.Key}";
                WriteJson(entry.Value, nationality.Item2, $"{outputDir}/vtubers.json");
            }

            (string latestFilePath, _) = FileUtility.GetLatestRecord(DATA_REPO_PATH, "top-videos");
            TopVideosList topVideoList = FileUtility.GetTopVideoList(latestFilePath);

            TopVideosListToJsonStruct videoTransformer = new(nationality.Item1);

            WriteJson(
                videoTransformer.Get(topVideoList, dictRecord, 100, allowDuplicate: true),
                nationality.Item2,
                "trending-videos/all.json"
            );

            WriteJson(
                videoTransformer.Get(topVideoList, dictRecord, 100, allowDuplicate: false),
                nationality.Item2,
                "trending-videos/no-duplicate.json"
            );

            LiveVideosListToJsonStruct liveVideosTransformer =
                new(nationality.Item1, currentTime: now);

            WriteJson(
                liveVideosTransformer.Get(liveVideos, dictRecord, noTitle: false),
                nationality.Item2,
                "livestreams/all.json"
            );

            WriteJson(
                liveVideosTransformer.Get(liveVideos, dictRecord, noTitle: true),
                nationality.Item2,
                "livestreams/all-no-title.json"
            );

            WriteJson(
                liveVideosTransformer.GetDebutToday(liveVideos, dictRecord, noTitle: false),
                nationality.Item2,
                "livestreams/debut.json"
            );

            WriteJson(
                liveVideosTransformer.GetDebutToday(liveVideos, dictRecord, noTitle: true),
                nationality.Item2,
                "livestreams/debut-no-title.json"
            );
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
        WriteJsonString(GetJsonString(updateTimeWrapper), "", outputFilePath);
    }

    private static void WriteJson(VTuberFullData vTuberFullData, string outputFilePath)
    {
        WriteJsonString(
            GetJsonString(new SingleVTuberFullDataResponse(VTuber: vTuberFullData)),
            "",
            outputFilePath
        );
    }

    private static void WriteJson(
        List<GenerateRecordList.Types.VTuberData> lstVTuberData,
        string nationality,
        string outputFilePath
    )
    {
        WriteJsonString(
            GetJsonString(new VTuberDataResponse(VTubers: lstVTuberData)),
            nationality,
            outputFilePath
        );
    }

    private static void WriteJson(
        List<VTuberViewCountGrowthData> lstVTuberData,
        string nationality,
        string outputFilePath
    )
    {
        WriteJsonString(
            GetJsonString(new VTuberViewCountChangeDataResponse(VTubers: lstVTuberData)),
            nationality,
            outputFilePath
        );
    }

    private static void WriteJson(
        List<VTuberGrowthData> lstVTuberData,
        string nationality,
        string outputFilePath
    )
    {
        WriteJsonString(
            GetJsonString(new VTuberGrowthDataResponse(VTubers: lstVTuberData)),
            nationality,
            outputFilePath
        );
    }

    private static void WriteJson(
        List<VideoPopularityData> lstVideoData,
        string nationality,
        string outputFilePath
    )
    {
        WriteJsonString(
            GetJsonString(new VideoPopularityDataResponse(videos: lstVideoData)),
            nationality,
            outputFilePath
        );
    }

    private static void WriteJson(
        List<VTuberDebutData> lstVTuberData,
        string nationality,
        string outputFilePath
    )
    {
        WriteJsonString(
            GetJsonString(new VTuberDebutDataResponse(VTubers: lstVTuberData)),
            nationality,
            outputFilePath
        );
    }

    private static void WriteJson(
        List<VTuberAnniversaryData> lstVTuberData,
        string nationality,
        string outputFilePath
    )
    {
        WriteJsonString(
            GetJsonString(new VTuberAnniversaryDataResponse(VTubers: lstVTuberData)),
            nationality,
            outputFilePath
        );
    }

    private static void WriteJson(
        List<VTuberGraduateData> lstVTuberData,
        string nationality,
        string outputFilePath
    )
    {
        WriteJsonString(
            GetJsonString(new VTuberGraduateDataResponse(VTubers: lstVTuberData)),
            nationality,
            outputFilePath
        );
    }

    private static void WriteJson(
        List<VTuberPopularityData> lstVTuberData,
        string nationality,
        string outputFilePath
    )
    {
        WriteJsonString(
            GetJsonString(new VTuberPopularityDataResponse(VTubers: lstVTuberData)),
            nationality,
            outputFilePath
        );
    }

    private static void WriteJson(
        List<GroupData> lstGroupData,
        string nationality,
        string outputFilePath
    )
    {
        WriteJsonString(
            GetJsonString(new GroupDataResponse(groups: lstGroupData)),
            nationality,
            outputFilePath
        );
    }

    private static void WriteJson(
        List<LivestreamData> lstLivestream,
        string nationality,
        string outputFilePath
    )
    {
        WriteJsonString(
            GetJsonString(new LivestreamDataResponse(livestreams: lstLivestream)),
            nationality,
            outputFilePath
        );
    }

    private static string GetJsonString(object obj)
    {
        return JsonSerializer.Serialize(obj, jsonSerializerOptions);
    }

    private static void WriteJsonString(
        string jsonString,
        string nationality,
        string outputFilePath
    )
    {
        string? outputFolder = Path.GetDirectoryName(
            Path.Combine(OUTPUT_PATH, nationality, outputFilePath)
        );
        if (outputFolder is not null && !Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        StreamWriter writer =
            new(Path.Combine(OUTPUT_PATH, nationality, outputFilePath)) { NewLine = "\n" };

        writer.Write(jsonString);
        writer.Close();
    }

    private static LiveVideosList ClearTwitchLiveVideos(LiveVideosList liveVideosList)
    {
        return new LiveVideosList().Insert(
            liveVideosList.Filter(e => !e.Url.StartsWith("https://www.twitch.tv/")).ToList()
        );
    }
}
