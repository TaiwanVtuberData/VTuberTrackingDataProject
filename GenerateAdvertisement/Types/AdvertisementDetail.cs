using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Globalization;
using LanguageExt;
using Microsoft.VisualBasic.FileIO;

namespace GenerateAdvertisement.Types;

public readonly record struct AdvertisementDetail(
    string Id,
    string DisplayName,
    string ImgUrl,
    Option<string> Url,
    TimeInterval TimeInterval,
    bool IsFirstPriority
)
{
    private static readonly System.Collections.Generic.HashSet<string> CSV_HEADER_SET =
    [
        "ID",
        "Display Name",
        "Image URL",
        "URL",
        "Start Time",
        "End Time",
        "Is First Priority",
    ];

    public static Validation<ValidationError, ImmutableList<AdvertisementDetail>> LoadFromCsv(
        Stream stream
    )
    {
        TextFieldParser reader =
            new(stream)
            {
                HasFieldsEnclosedInQuotes = true,
                Delimiters = [","],
                CommentTokens = ["#"],
                TrimWhiteSpace = false,
                TextFieldType = FieldType.Delimited,
            };

        string[]? headerBlock = reader.ReadFields();
        if (headerBlock is null)
        {
            return new ValidationError($"Invalid CSV header. Empty header.");
        }

        return BuildCsvHeaderIndexDict(CSV_HEADER_SET, headerBlock.ToList())
            .Map(csvHeaderIndexDict =>
            {
                List<Validation<ValidationError, AdvertisementDetail>> resultList = [];
                while (!reader.EndOfData)
                {
                    string[]? entryBlock = reader.ReadFields();

                    if (entryBlock is null)
                    {
                        continue;
                    }

                    resultList.Add(Validate(csvHeaderIndexDict, entryBlock.ToList()));
                }

                return resultList.Sequence().Map(list => list.ToImmutableList());
            })
            .Flatten();
    }

    private static Validation<ValidationError, AdvertisementDetail> Validate(
        Dictionary<string, int> csvHeaderIndexDict,
        List<string> entryList
    )
    {
        if (csvHeaderIndexDict.Count != entryList.Count)
        {
            return new ValidationError(
                $"entryList [${entryList}] does not match expected csvHeaderIndexDict [${csvHeaderIndexDict}] in size."
            );
        }

        return (
            ValidateTime(entryList[csvHeaderIndexDict["Start Time"]]),
            ValidateTime(entryList[csvHeaderIndexDict["End Time"]])
        )
            .Apply(
                (startTime, endTime) =>
                    (
                        ValidateId(entryList[csvHeaderIndexDict["ID"]]),
                        ValidateDisplayName(entryList[csvHeaderIndexDict["Display Name"]]),
                        ValidateImageUrl(entryList[csvHeaderIndexDict["Image URL"]]),
                        ValidateUrl(entryList[csvHeaderIndexDict["URL"]]),
                        TimeInterval.Validate(startTime, endTime),
                        ValidateIsFirstPriority(entryList[csvHeaderIndexDict["Is First Priority"]])
                    ).Apply(
                        (id, displayName, imgUrl, url, timeInterval, isFirstPriority) =>
                            new AdvertisementDetail(
                                Id: id,
                                DisplayName: displayName,
                                ImgUrl: imgUrl,
                                Url: url,
                                TimeInterval: timeInterval,
                                IsFirstPriority: isFirstPriority
                            )
                    )
            )
            .Flatten()
            .MapFail(error => new ValidationError(
                $"Error occured when processing data: {string.Join(',', entryList)}. Reason: {error.Value}"
            ));
    }

    private static Validation<ValidationError, string> ValidateId(string rawId)
    {
        if (rawId.Trim() != rawId)
        {
            return new ValidationError($"There is leading or trailing whitespce in ID: {rawId}");
        }

        if (rawId.Length != 32)
        {
            return new ValidationError(
                $"ID should be a valid UUID with lowercase and no '-': {rawId}"
            );
        }
        else
        {
            return rawId;
        }
    }

    private static Validation<ValidationError, string> ValidateDisplayName(string rawName)
    {
        if (rawName.Trim() != rawName)
        {
            return new ValidationError(
                $"There is leading or trailing whitespce in display name: {rawName}"
            );
        }

        if (rawName.Length == 0)
        {
            return new ValidationError($"Display name should not be empty: {rawName}");
        }
        else
        {
            return rawName;
        }
    }

    private static Validation<ValidationError, string> ValidateImageUrl(string rawImageUrl)
    {
        if (rawImageUrl.Trim() != rawImageUrl)
        {
            return new ValidationError(
                $"There is leading or trailing whitespce in image URL: {rawImageUrl}"
            );
        }

        if (rawImageUrl.Length == 0)
        {
            return new ValidationError($"Image URL should not be empty: {rawImageUrl}");
        }
        else
        {
            return rawImageUrl;
        }
    }

    private static Validation<ValidationError, Option<string>> ValidateUrl(string rawUrl)
    {
        if (rawUrl.Trim() != rawUrl)
        {
            return new ValidationError($"There is leading or trailing whitespce in URL: {rawUrl}");
        }

        if (rawUrl.Length == 0)
        {
            return Option<string>.None;
        }
        else
        {
            return Option<string>.Some(rawUrl);
        }
    }

    private static Validation<ValidationError, bool> ValidateIsFirstPriority(
        string rawIsFirstPriority
    )
    {
        if (rawIsFirstPriority.Trim() != rawIsFirstPriority)
        {
            return new ValidationError(
                $"There is leading or trailing whitespce in is first priority: {rawIsFirstPriority}"
            );
        }

        if (rawIsFirstPriority.Length == 0)
        {
            return false;
        }
        bool parseResult = bool.TryParse(rawIsFirstPriority, out bool isFirstPriority);

        if (parseResult == false)
        {
            return new ValidationError(
                $"Is first priority is not a valid boolean: {rawIsFirstPriority}"
            );
        }
        return isFirstPriority;
    }

    private static Validation<ValidationError, DateTimeOffset> ValidateTime(string rawTime)
    {
        if (rawTime.Trim() != rawTime)
        {
            return new ValidationError(
                $"There is leading or trailing whitespce in time: {rawTime}"
            );
        }

        if (rawTime.Length == 0)
        {
            return new ValidationError($"Time should not be empty: {rawTime}");
        }

        bool parseResult = DateTimeOffset.TryParseExact(
            rawTime,
            "yyyy-MM-ddTHH:mmZ",
            null,
            DateTimeStyles.AssumeUniversal,
            out DateTimeOffset time
        );

        if (parseResult == false)
        {
            return new ValidationError($"Time is not in valid ISO 8601 format: {rawTime}");
        }
        return time;
    }

    private static Validation<ValidationError, Dictionary<string, int>> BuildCsvHeaderIndexDict(
        System.Collections.Generic.HashSet<string> csvHeaderSet,
        List<string> headerList
    )
    {
        if (
            csvHeaderSet.Count != headerList.Count
            || csvHeaderSet.SetEquals(headerList.ToFrozenSet()) == false
        )
        {
            return new ValidationError(
                $"headerList [{ToString(headerList)}] does not match expected csvHeaderSet [{ToString(csvHeaderSet)}]."
            );
        }

        // initialize the Dictionary with keys as csvHeadeSet and values all set to -1
        Dictionary<string, int> csvHeaderIndexDict = csvHeaderSet.ToDictionary(e => e, e => -1);

        for (int i = 0; i < headerList.Count; i++)
        {
            csvHeaderIndexDict[headerList[i]] = i;
        }

        return csvHeaderIndexDict;
    }

    private static string ToString(IEnumerable<string> stringList)
    {
        return string.Join(',', stringList);
    }
}
