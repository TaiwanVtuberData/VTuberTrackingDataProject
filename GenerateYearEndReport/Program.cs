﻿using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Common.Types;
using Common.Types.Basic;
using Common.Utils;
using GenerateRecordList.Types;
using GenerateRecordList.Utils;
using GenerateYearEndReport;
using GenerateYearEndReport.Types;
using LanguageExt;

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
        string? CURRENT_TIME = Environment.GetEnvironmentVariable("CURRENT_TIME");
        Console.WriteLine($"CURRENT_TIME: {CURRENT_TIME}");

        string? DEBUT_DATE_THRESHOLD = Environment.GetEnvironmentVariable("DEBUT_DATE_THRESHOLD");
        Console.WriteLine($"DEBUT_DATE_THRESHOLD: {DEBUT_DATE_THRESHOLD}");

        string? DATA_REPO_DIRECTORY = Environment.GetEnvironmentVariable("DATA_REPO_DIRECTORY");
        Console.WriteLine($"DATA_REPO_DIRECTORY: {DATA_REPO_DIRECTORY}");

        string? OUTPUT_DIRECTORY = Environment.GetEnvironmentVariable("OUTPUT_DIRECTORY");
        Console.WriteLine($"OUTPUT_DIRECTORY: {OUTPUT_DIRECTORY}");

        if (
            CURRENT_TIME == null
            || DEBUT_DATE_THRESHOLD == null | DATA_REPO_DIRECTORY == null
            || OUTPUT_DIRECTORY == null
        )
        {
            Console.WriteLine(
                "Environment variables [CURRENT_TIME], [DEBUT_DATE_THRESHOLD], [DATA_REPO_DIRECTORY] and/or [OUTPUT_DIRECTORY] missing. Abort program."
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
                input: CURRENT_TIME,
                format: @"yyyy-MM-ddTHH:mm:ss",
                formatProvider: CultureInfo.InvariantCulture,
                styles: DateTimeStyles.AssumeLocal,
                result: out DateTimeOffset currentTime
            )
        )
        {
            Console.WriteLine(
                $"CURRENT_TIME [{CURRENT_TIME}] format ivaild. Expected: [yyyy-MM-ddTHH-mm-ss]. Abort program."
            );
            return;
        }
        Console.WriteLine($"Current Time is [{currentTime}].");

        if (
            !DateOnly.TryParseExact(
                s: DEBUT_DATE_THRESHOLD,
                format: @"yyyy-MM-dd",
                result: out DateOnly debutDateThreshold
            )
        )
        {
            Console.WriteLine(
                $"DEBUT_DATE_THRESHOLD [{DEBUT_DATE_THRESHOLD}] format ivaild. Expected: [yyyy-MM-dd]. Abort program."
            );
            return;
        }
        Console.WriteLine($"Debut Date Threshold is [{debutDateThreshold}].");

        // load data

        List<VTuberId> excluedList = FileUtility.GetListFromCsv(
            Path.Combine(DATA_REPO_DIRECTORY, "DATA/EXCLUDE_LIST.csv")
        );

        TrackList trackList =
            new(
                Path.Combine(DATA_REPO_DIRECTORY, "DATA/TW_VTUBER_TRACK_LIST.csv"),
                lstExcludeId: excluedList,
                throwOnValidationFail: true
            );

        (string latestBasicDataFilePath, DateTimeOffset latestBasicDataTime) =
            FileUtility.GetLatestRecord(
                directory: DATA_REPO_DIRECTORY,
                prefix: "basic-data",
                beforeDateTime: currentTime
            );

        Dictionary<VTuberId, VTuberBasicData> dictBasicData = VTuberBasicData.ReadFromCsv(
            latestBasicDataFilePath
        );

        (_, DateTimeOffset latestRecordTime) = FileUtility.GetLatestRecord(
            directory: DATA_REPO_DIRECTORY,
            prefix: "record",
            beforeDateTime: currentTime
        );

        // generate data
        DictionaryRecord dictRecord = new(trackList, excluedList, dictBasicData);

        MiscUtils.FillRecordOnlyNecessary(
            ref dictRecord,
            recordDir: DATA_REPO_DIRECTORY,
            targetTime: latestRecordTime,
            recentDays: 365
        );

        MiscUtils.FillBasicDataOnlyNecessary(
            ref dictRecord,
            basicDataDir: DATA_REPO_DIRECTORY,
            targetTime: latestBasicDataTime,
            recentDays: 365
        );

        // write result as JSON
        ClearAndCreateOutputFolder(OUTPUT_DIRECTORY);
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

            foreach (
                var tuple in new List<(int?, string)> { (10, "10"), (100, "100"), (null, "all") }
            )
            {
                WriteJson(
                    transformer.YouTubeGrowingVTubers(
                        count: tuple.Item1,
                        growingVTubersFilterOption: new DictionaryRecordToRecordList.GrowingVTubersFilterOption(
                            DictionaryRecordToRecordList.FilterOption.Before,
                            debutDateThreshold
                        )
                    ),
                    OUTPUT_DIRECTORY,
                    nationality.Item2,
                    $"growing-vtubers/youtube/established/{tuple.Item2}.json"
                );

                WriteJson(
                    transformer.YouTubeGrowingVTubers(
                        count: tuple.Item1,
                        growingVTubersFilterOption: new DictionaryRecordToRecordList.GrowingVTubersFilterOption(
                            DictionaryRecordToRecordList.FilterOption.AfterOrEqual,
                            debutDateThreshold
                        )
                    ),
                    OUTPUT_DIRECTORY,
                    nationality.Item2,
                    $"growing-vtubers/youtube/new/{tuple.Item2}.json"
                );

                WriteJson(
                    transformer.TwitchGrowingVTubers(
                        count: tuple.Item1,
                        growingVTubersFilterOption: new DictionaryRecordToRecordList.GrowingVTubersFilterOption(
                            DictionaryRecordToRecordList.FilterOption.Before,
                            debutDateThreshold
                        )
                    ),
                    OUTPUT_DIRECTORY,
                    nationality.Item2,
                    $"growing-vtubers/twitch/established/{tuple.Item2}.json"
                );

                WriteJson(
                    transformer.TwitchGrowingVTubers(
                        count: tuple.Item1,
                        growingVTubersFilterOption: new DictionaryRecordToRecordList.GrowingVTubersFilterOption(
                            DictionaryRecordToRecordList.FilterOption.AfterOrEqual,
                            debutDateThreshold
                        )
                    ),
                    OUTPUT_DIRECTORY,
                    nationality.Item2,
                    $"growing-vtubers/twitch/new/{tuple.Item2}.json"
                );

                WriteJson(
                    transformer.YouTubeVTubersViewCountChange(
                        count: tuple.Item1,
                        growingVTubersFilterOption: new DictionaryRecordToRecordList.GrowingVTubersFilterOption(
                            DictionaryRecordToRecordList.FilterOption.Before,
                            debutDateThreshold
                        )
                    ),
                    OUTPUT_DIRECTORY,
                    nationality.Item2,
                    $"vtubers-view-count-change/youtube/established/{tuple.Item2}.json"
                );

                WriteJson(
                    transformer.YouTubeVTubersViewCountChange(
                        count: tuple.Item1,
                        growingVTubersFilterOption: new DictionaryRecordToRecordList.GrowingVTubersFilterOption(
                            DictionaryRecordToRecordList.FilterOption.AfterOrEqual,
                            debutDateThreshold
                        )
                    ),
                    OUTPUT_DIRECTORY,
                    nationality.Item2,
                    $"vtubers-view-count-change/youtube/new/{tuple.Item2}.json"
                );
            }
        }

        Console.WriteLine("Program completed.");
    }

    private static void ClearAndCreateOutputFolder(string outputFolder)
    {
        if (Directory.Exists(outputFolder))
        {
            Directory.Delete(outputFolder, true);
        }
        Directory.CreateDirectory(outputFolder);
    }

    private static void WriteJson(
        List<YearEndVTuberYouTubeGrowthData> lstVTuberData,
        string outputDirectory,
        string nationality,
        string outputFilePath
    )
    {
        WriteJsonString(
            GetJsonString(new YearEndVTuberYouTubeGrowthDataResponse(VTubers: lstVTuberData)),
            outputDirectory,
            nationality,
            outputFilePath
        );
    }

    private static void WriteJson(
        List<YearEndVTuberTwitchGrowthData> lstVTuberData,
        string outputDirectory,
        string nationality,
        string outputFilePath
    )
    {
        WriteJsonString(
            GetJsonString(new YearEndVTuberTwitchGrowthDataResponse(VTubers: lstVTuberData)),
            outputDirectory,
            nationality,
            outputFilePath
        );
    }

    private static void WriteJson(
        List<YearEndVTuberYouTubeViewCountGrowthData> lstVTuberData,
        string outputDirectory,
        string nationality,
        string outputFilePath
    )
    {
        WriteJsonString(
            GetJsonString(new YearEndVTuberViewCountChangeDataResponse(VTubers: lstVTuberData)),
            outputDirectory,
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
}
