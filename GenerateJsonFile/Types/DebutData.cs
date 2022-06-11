using Microsoft.VisualBasic.FileIO;

namespace GenerateJsonFile.Types;

internal record DebutData(string Id, string VideoUrl, string ThumbnailUrl, DateTime StartTime)
{
    public static List<DebutData> ReadFromCsv(string csvFilePath)
    {
        if (!File.Exists(csvFilePath))
            return new();

        TextFieldParser reader = new(csvFilePath)
        {
            HasFieldsEnclosedInQuotes = true,
            Delimiters = new string[] { "," },
            CommentTokens = new string[] { "#" },
            TrimWhiteSpace = false,
            TextFieldType = FieldType.Delimited,
        };

        // consume header
        string[]? headerBlock = reader.ReadFields();

        if (headerBlock is null || headerBlock.Length != 4)
            return new();

        List<DebutData> rLst = new();

        while (!reader.EndOfData)
        {
            string[]? entryBlock = reader.ReadFields();

            if (entryBlock is null)
            {
                return new();
            }

            rLst.Add(new DebutData(Id: entryBlock[0], VideoUrl: entryBlock[1], ThumbnailUrl: entryBlock[2], StartTime: DateTime.Parse(entryBlock[3])));
        }

        return rLst;
    }
}
