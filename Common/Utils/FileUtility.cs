using Common.Types;
using Microsoft.VisualBasic.FileIO;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Common.Utils;
public class FileUtility
{
    public static List<Tuple<FileInfo, DateTime>> GetFileInfoDateTimeList(string directory, string prefix, int recentDays)
    {
        List<Tuple<FileInfo, DateTime>> fileInfoDateTimeList = new();

        // format: record_2021-02-21-21-52-13.csv
        string pattern = $"^{prefix}_(?<Date>[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]-[0-9][0-9]-[0-9][0-9]-[0-9][0-9]).csv$";
        Regex fileNameRegex = new(pattern);
        DateTime latestDateTime = DateTime.UnixEpoch;
        foreach (string fileName in Directory.GetFiles(directory, "*.csv", System.IO.SearchOption.AllDirectories))
        {
            FileInfo fileInfo = new(fileName);

            Match match = fileNameRegex.Match(fileInfo.Name);
            if (!match.Success)
            {
                // Console.WriteLine(fileName + " does not match the pattern.");
                continue;
            }

            string dateString = match.Groups["Date"].Value;
            DateTime parsedDateTime;
            if (!DateTime.TryParseExact(dateString, @"yyyy-MM-dd-HH-mm-ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDateTime))
                continue;
            parsedDateTime = DateTime.SpecifyKind(parsedDateTime, DateTimeKind.Local);

            if (parsedDateTime > latestDateTime)
            {
                latestDateTime = parsedDateTime;
            }

            fileInfoDateTimeList.Add(new Tuple<FileInfo, DateTime>(fileInfo, parsedDateTime));
        }

        if (recentDays > 0)
        {
            // add 3 hours buffer
            TimeSpan recentDaysBuffer = new(days: recentDays, hours: 3, minutes: 0, seconds: 0);
            fileInfoDateTimeList.RemoveAll(s => (latestDateTime - s.Item2).Duration() > recentDaysBuffer);
        }

        return fileInfoDateTimeList;
    }

    public static string GetSingleLineFromFile(string filePath)
    {
        string? line;

        // Read the file and display it line by line.  
        using StreamReader file = new(filePath);
        if ((line = file.ReadLine()) != null)
        {
            file.Close();
            return line;
        }

        throw new Exception($"Could not retrieve line from file {filePath}.");
    }

    public static Tuple<string, DateTime> GetLatestRecord(string directory, string prefix)
    {
        string pattern = $"^{prefix}_(?<Date>[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]-[0-9][0-9]-[0-9][0-9]-[0-9][0-9]).csv$";
        // format: record_2021-02-21-21-52-13.csv
        Regex fileNameRegex = new(pattern);
        DateTime latestDateTime = DateTime.UnixEpoch;
        string latestRecordFilePath = "";
        foreach (string fileName in Directory.GetFiles(directory, "*.csv", System.IO.SearchOption.AllDirectories))
        {
            FileInfo fileInfo = new(fileName);

            Match match = fileNameRegex.Match(fileInfo.Name);
            if (!match.Success)
            {
                // Console.WriteLine(fileName + " does not match the pattern.");
                continue;
            }

            string dateString = match.Groups["Date"].Value;
            DateTime parsedDateTime;
            if (!DateTime.TryParseExact(dateString, @"yyyy-MM-dd-HH-mm-ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDateTime))
                continue;
            parsedDateTime = DateTime.SpecifyKind(parsedDateTime, DateTimeKind.Local);

            if (parsedDateTime > latestDateTime)
            {
                latestDateTime = parsedDateTime;
                latestRecordFilePath = fileName;
            }
        }

        return new Tuple<string, DateTime>(latestRecordFilePath, latestDateTime);
    }

    public static List<string> GetListFromCsv(string filePath)
    {
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

        List<string> rList = new();
        while (!reader.EndOfData)
        {
            string[]? entryBlock = reader.ReadFields();
            if (entryBlock is null || entryBlock.Length != 1)
            {
                continue;
            }

            rList.Add(entryBlock[0]);
        }

        return rList;
    }

    public static Dictionary<string, string> GetDictFromCsv(string filePath)
    {
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

        Dictionary<string, string> rDict = new();
        while (!reader.EndOfData)
        {
            string[]? entryBlock = reader.ReadFields();
            if (entryBlock is null || entryBlock.Length != 2)
            {
                continue;
            }

            rDict.Add(entryBlock[0], entryBlock[1]);
        }

        return rDict;
    }

    public static TopVideosList GetTopVideoList(string filePath)
    {
        // CSV Format:
        // Display Name,View Count,Title,Publish Time,URL,Thumbnail URL
        // 璐洛洛,39127,【原神研究室】五郎全分析🐶▸提供珍貴岩元素暴傷，只為岩系隊伍輔助的忠犬！聖遺物/命座建議/天賦/武器/組隊搭配 ▹璐洛洛◃,2021-12-21T13:30:15Z,https://www.youtube.com/watch?v=NOHX-uAJ2Xg,https://i.ytimg.com/vi/NOHX-uAJ2Xg/default.jpg
        // 杏仁ミル,35115,跟壞朋友們迎接2022年!!!!!!!,2021 - 12 - 31T18: 58:28Z,https://www.youtube.com/watch?v=gMyV3wvn4bg,https://i.ytimg.com/vi/gMyV3wvn4bg/default.jpg

        TextFieldParser reader = new(filePath)
        {
            HasFieldsEnclosedInQuotes = true,
            Delimiters = new string[] { "," },
            TrimWhiteSpace = false,
            TextFieldType = FieldType.Delimited,
        };

        // consume header
        string[]? headerBlock = reader.ReadFields();
        if (headerBlock is null || headerBlock.Length != 6)
        {
            return new();
        }

        TopVideosList rList = new();

        while (!reader.EndOfData)
        {
            string[]? entryBlock = reader.ReadFields();

            if (entryBlock is not null)
                rList.Insert(entryBlock);
        }

        return rList;
    }
}
