using System.Collections.Immutable;
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Common.Types;
using Common.Types.Basic;
using Common.Utils;
using FetchYouTubeRecord;
using GenerateJsonFile;
using GenerateJsonFile.Types;
using GenerateRecordList.Types;

class Program
{
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

    static void Main()
    {
        string? YOUTUBE_API_KEY = Environment.GetEnvironmentVariable("YOUTUBE_API_KEY");

        string? END_TIME = Environment.GetEnvironmentVariable("END_TIME");
        Console.WriteLine($"END_TIME: {END_TIME}");

        string? DEBUT_DATE_THRESHOLD = Environment.GetEnvironmentVariable("DEBUT_DATE_THRESHOLD");
        Console.WriteLine($"DEBUT_DATE_THRESHOLD: {DEBUT_DATE_THRESHOLD}");

        string? DATA_REPO_DIRECTORY = Environment.GetEnvironmentVariable("DATA_REPO_DIRECTORY");
        Console.WriteLine($"DATA_REPO_DIRECTORY: {DATA_REPO_DIRECTORY}");

        string? OUTPUT_DIRECTORY = Environment.GetEnvironmentVariable("OUTPUT_DIRECTORY");
        Console.WriteLine($"OUTPUT_DIRECTORY: {OUTPUT_DIRECTORY}");

        if (
            YOUTUBE_API_KEY == null
            || END_TIME == null
            || DEBUT_DATE_THRESHOLD == null
            || DATA_REPO_DIRECTORY == null
            || OUTPUT_DIRECTORY == null
        )
        {
            Console.WriteLine(
                "Environment variables [YOUTUBE_API_KEY], [END_TIME], [DEBUT_DATE_THRESHOLD], [DATA_REPO_DIRECTORY] and/or [OUTPUT_DIRECTORY] missing. Abort program."
            );
            return;
        }

        if (Directory.Exists(DATA_REPO_DIRECTORY) == false)
        {
            Console.WriteLine(
                $"DATA_REPO_DIRECTORY [{DATA_REPO_DIRECTORY}] does not exist. Abort program."
            );
            return;
        }

        if (
            !DateTimeOffset.TryParseExact(
                input: END_TIME,
                format: @"yyyy-MM-ddTHH:mm:ss",
                formatProvider: CultureInfo.InvariantCulture,
                styles: DateTimeStyles.AssumeLocal,
                result: out DateTimeOffset endTime
            )
        )
        {
            Console.WriteLine(
                $"END_TIME [{END_TIME}] format invalid. Expected: [yyyy-MM-ddTHH-mm-ss]. Abort program."
            );
            return;
        }
        Console.WriteLine($"End Time is [{END_TIME}].");

        if (
            !DateOnly.TryParseExact(
                s: DEBUT_DATE_THRESHOLD,
                format: @"yyyy-MM-dd",
                result: out DateOnly debutDateThreshold
            )
        )
        {
            Console.WriteLine(
                $"DEBUT_DATE_THRESHOLD [{DEBUT_DATE_THRESHOLD}] format invalid. Expected: [yyyy-MM-dd]. Abort program."
            );
            return;
        }
        Console.WriteLine($"Debut Date Threshold is [{debutDateThreshold}].");

        // load data
        List<VTuberId> excludeList = FileUtility.GetListFromCsv(
            Path.Combine(DATA_REPO_DIRECTORY, "DATA/EXCLUDE_LIST.csv")
        );

        TrackList trackList =
            new(
                Path.Combine(DATA_REPO_DIRECTORY, "DATA/TW_VTUBER_TRACK_LIST.csv"),
                lstExcludeId: excludeList,
                ignoreGraduated: false,
                throwOnValidationFail: true
            );
        ImmutableList<YouTubeChannelId> lstYouTubeChannelId =
        [
            .. trackList.GetYouTubeChannelIdList().Map(e => new YouTubeChannelId(e)),
        ];
        Console.WriteLine($"Channel count: {lstYouTubeChannelId.Count}");

        (string latestBasicDataFilePath, DateTimeOffset latestBasicDataTime) =
            FileUtility.GetLatestRecord(
                directory: DATA_REPO_DIRECTORY,
                prefix: "basic-data",
                beforeDateTime: endTime
            );

        Dictionary<VTuberId, VTuberBasicData> dictBasicData = VTuberBasicData.ReadFromCsv(
            latestBasicDataFilePath
        );

        (_, DateTimeOffset latestRecordTime) = FileUtility.GetLatestRecord(
            directory: DATA_REPO_DIRECTORY,
            prefix: "record",
            beforeDateTime: endTime
        );

        // generate data
        DictionaryRecord dictRecord = new(trackList, excludeList, dictBasicData);
        Console.WriteLine($"dictBasicData count: {dictBasicData.Count}");

        // init FetchYouTubeStatistics.Fetcher
        Fetcher youtubeDataFetcher = new(YOUTUBE_API_KEY, endTime);

        // daysLimit set to 2 years
        int DAYS_LIMIT = 730;
        TopVideosList topVideoList;
        (_, topVideoList, _) = youtubeDataFetcher.GetAll(
            lstYouTubeChannelId,
            getDaysLimit: DAYS_LIMIT,
            filterDaysLimit: DAYS_LIMIT
        );

        foreach (VideoInformation videoInfo in topVideoList)
        {
            try
            {
                videoInfo.Id = trackList.GetIdByYouTubeChannelId(videoInfo.Id.Value);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error while converting topVideoList");
                Console.WriteLine($"GetIdByYouTubeChannelId with input {videoInfo.Id}");
                Console.WriteLine(e.Message, e);
            }
        }
        Console.WriteLine($"topVideoList count: {topVideoList.GetSortedList().Count}");

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
            TopVideosListToJsonStruct videoTransformer = new(nationality.Item1);

            foreach (
                var tuple in new List<(int, string)> { (10, "10"), (100, "100"), (1000, "1000") }
            )
            {
                WriteJson(
                    videoTransformer.Get(
                        topVideoList: topVideoList,
                        dictRecord: dictRecord,
                        count: tuple.Item1,
                        allowDuplicate: false,
                        vTuberFilterOption: new TopVideosListToJsonStruct.VTuberFilterOption(
                            TopVideosListToJsonStruct.FilterOption.Before,
                            debutDateThreshold
                        )
                    ),
                    outputDirectory: OUTPUT_DIRECTORY,
                    nationality.Item2,
                    $"trending-videos/youtube/established/{tuple.Item2}.json"
                );

                WriteJson(
                    videoTransformer.Get(
                        topVideoList: topVideoList,
                        dictRecord: dictRecord,
                        count: tuple.Item1,
                        allowDuplicate: false,
                        vTuberFilterOption: new TopVideosListToJsonStruct.VTuberFilterOption(
                            TopVideosListToJsonStruct.FilterOption.AfterOrEqual,
                            debutDateThreshold
                        )
                    ),
                    outputDirectory: OUTPUT_DIRECTORY,
                    nationality.Item2,
                    $"trending-videos/youtube/new/{tuple.Item2}.json"
                );
            }
        }
    }

    private static void WriteJson(
        List<VideoPopularityData> lstVideoData,
        string outputDirectory,
        string nationality,
        string outputFilePath
    )
    {
        WriteJsonString(
            GetJsonString(new VideoPopularityDataResponse(videos: lstVideoData)),
            outputDirectory,
            nationality,
            outputFilePath
        );
    }

    private static void WriteJsonString(
        string jsonString,
        string outputDirectory,
        string nationality,
        string outputFilePath
    )
    {
        string? outputFolder = Path.GetDirectoryName(
            Path.Combine(outputDirectory, nationality, outputFilePath)
        );
        if (outputFolder is not null && !Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        StreamWriter writer =
            new(Path.Combine(outputDirectory, nationality, outputFilePath)) { NewLine = "\n" };

        writer.Write(jsonString);
        writer.Close();
    }

    private static string GetJsonString(object obj)
    {
        return JsonSerializer.Serialize(obj, jsonSerializerOptions);
    }
}
