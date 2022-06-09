using Microsoft.VisualBasic.FileIO;

namespace GenerateJsonFile.Types;

internal record DebutData(string YouTubeId, string VideoId, DateTime StartTime)
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

        if (headerBlock is null || headerBlock.Length != 3)
            return new();

        List<DebutData> rLst = new();

        while (!reader.EndOfData)
        {
            string[]? entryBlock = reader.ReadFields();

            if (entryBlock is null)
            {
                return new();
            }

            rLst.Add(new DebutData(YouTubeId: entryBlock[0], VideoId: entryBlock[1], StartTime: DateTime.Parse(entryBlock[2])));
        }

        return rLst;
    }
}
