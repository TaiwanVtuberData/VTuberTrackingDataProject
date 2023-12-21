using Common.Types.Basic;
using Common.Utils;
using Microsoft.VisualBasic.FileIO;
using System.Text.Json.Serialization;

namespace GenerateRecordList.Types;

public record DebutData(
    [property: JsonConverter(typeof(VTuberIdJsonConverter))] VTuberId Id,
    string VideoUrl,
    string ThumbnailUrl,
    DateTime StartTime) {
    public static List<DebutData> ReadFromCsv(string csvFilePath) {
        if (!File.Exists(csvFilePath))
            return [];

        TextFieldParser reader = new(csvFilePath) {
            HasFieldsEnclosedInQuotes = true,
            Delimiters = [","],
            CommentTokens = ["#"],
            TrimWhiteSpace = false,
            TextFieldType = FieldType.Delimited,
        };

        // consume header
        string[]? headerBlock = reader.ReadFields();

        if (headerBlock is null || headerBlock.Length != 4)
            return [];

        List<DebutData> rLst = [];

        while (!reader.EndOfData) {
            string[]? entryBlock = reader.ReadFields();

            if (entryBlock is null) {
                return [];
            }

            rLst.Add(
                new DebutData(
                    Id: new VTuberId(entryBlock[0]),
                    VideoUrl: entryBlock[1],
                    ThumbnailUrl: entryBlock[2],
                    StartTime: DateTime.Parse(entryBlock[3])
                    )
                );
        }

        return rLst;
    }
}
