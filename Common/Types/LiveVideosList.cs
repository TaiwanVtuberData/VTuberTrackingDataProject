using Common.Types.Basic;
using LanguageExt;
using Microsoft.VisualBasic.FileIO;
using System.Xml;

namespace Common.Types;

public class LiveVideosList : List<LiveVideoInformation> {
    private static readonly Dictionary<string, int> csvHeaderIndexs = new()
    {
        { "VTuber ID", 0 },
        { "Video Type", 1 },
        { "Title", 2 },
        { "Publish Time", 3 },
        { "URL", 4 },
        { "Thumbnail URL", 5 },
    };

    public LiveVideosList() {
    }

    public LiveVideosList(string csvFilePath, bool clearGarbage, bool throwOnValidationFail) {
        Validation<ValidationError, List<LiveVideoInformation>> loadResult = LoadAndValidate(csvFilePath);

        loadResult.Match(
            result => {
                if (clearGarbage) {
                    this.AddRange(ClearGarbage(result));
                } else {
                    this.AddRange(result);
                }
            },
            error => {
                if (throwOnValidationFail)
                    throw new Exception($"Failed to load LiveVideosList: {csvFilePath}");
            }
            );
    }

    public static Validation<ValidationError, List<LiveVideoInformation>> LoadAndValidate(string csvFilePath) {
        TextFieldParser reader = new(csvFilePath) {
            HasFieldsEnclosedInQuotes = true,
            Delimiters = new string[] { "," },
            TrimWhiteSpace = false,
            TextFieldType = FieldType.Delimited,
        };

        // consume header
        string[]? headerBlock = reader.ReadFields();

        if (headerBlock is null) {
            return new ValidationError($"Invalid CSV header. Empty header.");
        }

        if (headerBlock.Length != csvHeaderIndexs.Count) {
            return new ValidationError($"Invalid CSV header. Expected: {csvHeaderIndexs.Count}. Actual: {headerBlock.Length}");
        }


        List<Validation<ValidationError, LiveVideoInformation>> lstResult = new();
        while (!reader.EndOfData) {
            string[]? entryBlock = reader.ReadFields();

            if (entryBlock is not null)
                lstResult.Add(Validate(entryBlock));
        }

        return lstResult.
            Sequence()
            .Match<Validation<ValidationError, List<LiveVideoInformation>>>(
            lstData => {
                return lstData.ToList();
            },
            errors => {
                return errors;
            });
    }

    private static Validation<ValidationError, LiveVideoInformation> Validate(string[] entryBlock) {
        return (
            VTuberId.Validate(entryBlock[csvHeaderIndexs["VTuber ID"]]),
            ValidateVideoType(entryBlock[csvHeaderIndexs["Video Type"]]),
            ValidateTitle(entryBlock[csvHeaderIndexs["Title"]]),
            ValidatePublishTime(entryBlock[csvHeaderIndexs["Publish Time"]]),
            ValidateUrl(entryBlock[csvHeaderIndexs["URL"]]),
            ValidateUrl(entryBlock[csvHeaderIndexs["Thumbnail URL"]])
            ).Apply(
            (
                id,
                videoType,
                title,
                publishTime,
                url,
                thumbnailUrl
                ) => new LiveVideoInformation() {
                    Id = id,
                    VideoType = videoType,
                    Title = title,
                    PublishDateTime = publishTime,
                    Url = url,
                    ThumbnailUrl = thumbnailUrl
                }
                );
    }

    private static Validation<ValidationError, LiveVideoType> ValidateVideoType(string rawType) {
        bool isValid = Enum.TryParse(rawType, out LiveVideoType parsedType);

        if (!isValid) {
            return new ValidationError($"Invalid Video Type: {rawType}");
        } else {
            return parsedType;
        }
    }

    private static Validation<ValidationError, string> ValidateTitle(string rawTitle) {
        return rawTitle.ReplaceLineEndings(" ");
    }

    private static Validation<ValidationError, DateTime> ValidatePublishTime(string rawTime) {
        try {
            // This is actually the best ISO 8601 string parser.
            return XmlConvert.ToDateTime(rawTime, XmlDateTimeSerializationMode.Utc);
        } catch {
            return new ValidationError($"Invalid Publish Time: {rawTime}");
        }
    }

    private static Validation<ValidationError, string> ValidateUrl(string rawUrl) {
        return rawUrl;
    }

    private static List<LiveVideoInformation> ClearGarbage(List<LiveVideoInformation> input) {
        return input
            .Filter(e => !IsDeadLivestream(e))
            .Filter(e => !IsScheduledLivestream(e))
            .ToList();
    }

    private static bool IsDeadLivestream(LiveVideoInformation e) {
        return e.PublishDateTime == DateTime.UnixEpoch && e.VideoType == LiveVideoType.upcoming;
    }

    private static bool IsScheduledLivestream(LiveVideoInformation e) {
        return e.ThumbnailUrl == "";
    }

    public LiveVideosList Insert(IEnumerable<LiveVideoInformation> enumerable) {
        this.AddRange(enumerable);

        return this;
    }
}
