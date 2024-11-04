using System.Collections.Immutable;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using GenerateAdvertisement.Types;
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
        string? SCHEDULE_CSV_PATH = Environment.GetEnvironmentVariable("SCHEDULE_CSV_PATH");
        Console.WriteLine($"SCHEDULE_CSV_PATH: {SCHEDULE_CSV_PATH}");

        string? OUTPUT_DIRECTORY = Environment.GetEnvironmentVariable("OUTPUT_DIRECTORY");
        Console.WriteLine($"OUTPUT_DIRECTORY: {OUTPUT_DIRECTORY}");

        if (SCHEDULE_CSV_PATH == null || OUTPUT_DIRECTORY == null)
        {
            Console.WriteLine(
                "Environment variables [SCHEDULE_CSV_PATH] and/or [OUTPUT_DIRECTORY] missing. Abort program."
            );
            return;
        }

        if (File.Exists(SCHEDULE_CSV_PATH) == false)
        {
            Console.WriteLine(
                $"SCHEDULE_CSV_PATH [{SCHEDULE_CSV_PATH}] does not exist. Abort program."
            );
            return;
        }

        DateTimeOffset currentTime = DateTimeOffset.Now;
        Console.WriteLine($"Current Time is [{currentTime}].");

        ImmutableList<AdvertisementDetail> advertisementDetailList =
            GetAdvertisementDetailListOrThrow(SCHEDULE_CSV_PATH);

        Option<AdvertisementDetail> currentAdvertisementDetail = GetCurrentAdvertisementDetail(
            advertisementDetailList,
            currentTime
        );

        AdvertisementResponse response = OptionAdvertisementDetailToAdvertisementResponse(
            currentAdvertisementDetail
        );

        ClearAndCreateDirectory(Path.Combine(OUTPUT_DIRECTORY, "api"));

        string outputFilePath = Path.Combine(
            OUTPUT_DIRECTORY,
            "api/v1/advertisements/current.json"
        );

        Console.WriteLine($"Output file path is [{outputFilePath}]");

        WriteJson(response, outputFilePath);

        Console.WriteLine("Program completed.");
    }

    private static ImmutableList<AdvertisementDetail> GetAdvertisementDetailListOrThrow(
        string filePath
    )
    {
        using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read);

        return AdvertisementDetail
            .LoadFromCsv(fileStream)
            .Match(
                advertisementDetailList =>
                {
                    return advertisementDetailList;
                },
                errors =>
                {
                    Console.WriteLine(errors.Aggregate("", (a, b) => a + "\n" + b));
                    throw new Exception(
                        $"Failed to load AdvertisementDetail list from file: {filePath}"
                    );
                }
            );
    }

    private static Option<AdvertisementDetail> GetCurrentAdvertisementDetail(
        ImmutableList<AdvertisementDetail> advertisementDetailList,
        DateTimeOffset currentTime
    )
    {
        IEnumerable<AdvertisementDetail> currentAdvertisementDetailList =
            advertisementDetailList.Where(e => e.TimeInterval.Within(currentTime));

        if (currentAdvertisementDetailList.Any() == false)
        {
            return Option<AdvertisementDetail>.None;
        }

        Option<AdvertisementDetail> firstPriorityAdvertisementDetail =
            currentAdvertisementDetailList.Find(e => e.IsFirstPriority);

        return firstPriorityAdvertisementDetail.IfNone(currentAdvertisementDetailList.First());
    }

    private static AdvertisementResponse OptionAdvertisementDetailToAdvertisementResponse(
        Option<AdvertisementDetail> optionAdvertisementDetail
    )
    {
        return optionAdvertisementDetail.Match(
            Some: advertisementDetail => new AdvertisementResponse(
                hasAdvertisement: true,
                advertisement: new Advertisement(
                    imgUrl: advertisementDetail.ImgUrl,
                    url: OptionToNullable(advertisementDetail.Url)
                )
            ),
            None: new AdvertisementResponse(hasAdvertisement: false, advertisement: null)
        );
    }

    private static string? OptionToNullable(Option<string> optionalValue)
    {
        return optionalValue.MatchUnsafe(None: () => null, Some: v => v);
    }

    private static void ClearAndCreateDirectory(string directory)
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
        Directory.CreateDirectory(directory);
    }

    private static void WriteJson(
        AdvertisementResponse advertisementResponse,
        string outputFilePath
    )
    {
        string? outputFolder = Path.GetDirectoryName(Path.Combine(outputFilePath));

        if (outputFolder is not null && !Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);

            StreamWriter writer = new(outputFilePath) { NewLine = "\n" };

            writer.Write(GetJsonString(advertisementResponse));
            writer.Close();
        }
    }

    private static string GetJsonString(object obj)
    {
        return JsonSerializer.Serialize(obj, jsonSerializerOptions);
    }
}
