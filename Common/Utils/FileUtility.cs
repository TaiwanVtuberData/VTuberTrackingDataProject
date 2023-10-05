using Common.Types;
using Common.Types.Basic;
using Microsoft.VisualBasic.FileIO;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Common.Utils;
public class FileUtility {
    public static List<Tuple<FileInfo, DateTime>> GetFileInfoDateTimeList(string parentDirectory, string prefix, DateTime targetDate, int recentDays) {

        List<string> dierctoryList = FilterYearOfMonthOfInterest(Directory.GetDirectories(parentDirectory), targetDate, recentDays);

        List<Tuple<FileInfo, DateTime>> fileInfoDateTimeList = dierctoryList
          .Map(directory => GetFileInfoDateTimeListNotRecursive(directory, prefix))
          // this can flatten list somehow???
          .SelectMany(e => e)
          .ToList();

        DateTime latestDateTime = fileInfoDateTimeList.Max(e => e.Item2);

        if (recentDays > 0) {
            // add 3 hours buffer
            TimeSpan recentDaysBuffer = new(days: recentDays, hours: 3, minutes: 0, seconds: 0);
            fileInfoDateTimeList.RemoveAll(s => (targetDate - s.Item2).Duration() > recentDaysBuffer);
        }

        return fileInfoDateTimeList;
    }

    // filter a list of year/month with recentDays so that only the
    // most recent month in the list minus recentDays is preserved
    // Example:
    // yearMonthList [2022-05, 2022-06, 2022-07]
    // recentDays: 35
    // Output: [2022-06, 2022-07]
    private static List<string> FilterYearOfMonthOfInterest(IEnumerable<string> directories, DateTime targetDate, int recentDays) {
        // format: 2022-09
        string pattern = $"^[0-9][0-9][0-9][0-9]-[0-9][0-9]$";
        Regex regex = new(pattern);

        // first item: xxx/2022-01, second item 2022-01
        List<Tuple<string, string>> filtertedList = directories
          .Map(
          e => new Tuple<string, string>(e, new DirectoryInfo(e).Name)
          )
          .Filter(directoryTuple =>
           regex.IsMatch(directoryTuple.Item2)
        ).ToList();

        // year/month string is sortable
        string? latestMonthString = filtertedList.Max(e => e.Item2);
        if (latestMonthString is null) {
            return new List<string>();
        }

        // add one more day just to be sure
        DateTime targetTime = targetDate.AddMonths(-1).AddDays(-(recentDays + 1));
        string targetMonthString = targetTime.ToString(@"yyyy-MM");

        // only reserve striing that is greater or equal to targetMonthString
        return filtertedList
          .Filter(e => e.Item2.CompareTo(targetMonthString) >= 0)
          .Map(e => e.Item1)
          .ToList();
    }

    private static DateTime GetLastMomentOfMonth(DateTime dateTime) =>
      new(
        year: dateTime.Year,
        month: dateTime.Month,
        day: DateTime.DaysInMonth(dateTime.Year, dateTime.Month),
        hour: 23,
        minute: 59,
        second: 59,
        millisecond: 999,
        kind: DateTimeKind.Local
        );

    private static List<Tuple<FileInfo, DateTime>> GetFileInfoDateTimeListNotRecursive(string directory, string prefix) {
        List<Tuple<FileInfo, DateTime>> fileInfoDateTimeList = new();

        // format: record_2021-02-21-21-52-13.csv
        string pattern = $"^{prefix}_(?<Date>[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]-[0-9][0-9]-[0-9][0-9]-[0-9][0-9]).csv$";
        Regex fileNameRegex = new(pattern);
        DateTime latestDateTime = DateTime.UnixEpoch;
        foreach (string fileName in Directory.GetFiles(directory, "*.csv", System.IO.SearchOption.AllDirectories)) {
            FileInfo fileInfo = new(fileName);

            Match match = fileNameRegex.Match(fileInfo.Name);
            if (!match.Success) {
                // Console.WriteLine(fileName + " does not match the pattern.");
                continue;
            }

            string dateString = match.Groups["Date"].Value;
            DateTime parsedDateTime;
            if (!DateTime.TryParseExact(dateString, @"yyyy-MM-dd-HH-mm-ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDateTime))
                continue;
            parsedDateTime = DateTime.SpecifyKind(parsedDateTime, DateTimeKind.Local);

            if (parsedDateTime > latestDateTime) {
                latestDateTime = parsedDateTime;
            }

            fileInfoDateTimeList.Add(new Tuple<FileInfo, DateTime>(fileInfo, parsedDateTime));
        }

        return fileInfoDateTimeList;
    }

    public static string GetSingleLineFromFile(string filePath) {
        string? line;

        // Read the file and display it line by line.  
        using StreamReader file = new(filePath);
        if ((line = file.ReadLine()) != null) {
            file.Close();
            return line;
        }

        throw new Exception($"Could not retrieve line from file {filePath}.");
    }

    public static Tuple<string, DateTime> GetLatestRecord(string directory, string prefix) {
        string pattern = $"^{prefix}_(?<Date>[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]-[0-9][0-9]-[0-9][0-9]-[0-9][0-9]).csv$";
        // format: record_2021-02-21-21-52-13.csv
        Regex fileNameRegex = new(pattern);
        DateTime latestDateTime = DateTime.UnixEpoch;
        string latestRecordFilePath = "";
        foreach (string fileName in Directory.GetFiles(directory, "*.csv", System.IO.SearchOption.AllDirectories)) {
            FileInfo fileInfo = new(fileName);

            Match match = fileNameRegex.Match(fileInfo.Name);
            if (!match.Success) {
                // Console.WriteLine(fileName + " does not match the pattern.");
                continue;
            }

            string dateString = match.Groups["Date"].Value;
            DateTime parsedDateTime;
            if (!DateTime.TryParseExact(dateString, @"yyyy-MM-dd-HH-mm-ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDateTime))
                continue;
            parsedDateTime = DateTime.SpecifyKind(parsedDateTime, DateTimeKind.Local);

            if (parsedDateTime > latestDateTime) {
                latestDateTime = parsedDateTime;
                latestRecordFilePath = fileName;
            }
        }

        return new Tuple<string, DateTime>(latestRecordFilePath, latestDateTime);
    }

    public static Tuple<string, DateTime> GetRecordAndDateDifference(string directory, string prefix, DateTime target, TimeSpan timeSpan) {
        string pattern = $"^{prefix}_(?<Date>[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]-[0-9][0-9]-[0-9][0-9]-[0-9][0-9]).csv$";
        // format: record_2021-02-21-21-52-13.csv
        Regex fileNameRegex = new(pattern);

        List<string> filePathList = new();
        List<DateTime> dateTimeList = new();
        foreach (string fileName in Directory.GetFiles(directory, "*.csv", System.IO.SearchOption.AllDirectories)) {
            FileInfo fileInfo = new(fileName);

            Match match = fileNameRegex.Match(fileInfo.Name);
            if (!match.Success) {
                // Console.WriteLine(fileName + " does not match the pattern.");
                continue;
            }

            string dateString = match.Groups["Date"].Value;
            DateTime parsedDateTime;
            if (!DateTime.TryParseExact(dateString, @"yyyy-MM-dd-HH-mm-ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDateTime))
                continue;
            parsedDateTime = DateTime.SpecifyKind(parsedDateTime, DateTimeKind.Local);

            filePathList.Add(fileName);
            dateTimeList.Add(parsedDateTime);
        }

        int closestIndex = FindClosestIndex(dateTimeList, target, timeSpan);

        return new Tuple<string, DateTime>(filePathList[closestIndex], dateTimeList[closestIndex]);
    }

    private static int FindClosestIndex(List<DateTime> dateTimes, DateTime target, TimeSpan timeSpan) {
        TimeSpan minTimeSpan = TimeSpan.MaxValue;
        int minIndex = -1;
        for (int i = 0; i < dateTimes.Count; i++) {
            TimeSpan timeDifference = (timeSpan - (target - dateTimes[i]).Duration()).Duration();
            if (minTimeSpan > timeDifference) {
                minTimeSpan = timeDifference;
                minIndex = i;
            }
        }

        return minIndex;
    }

    public static List<VTuberId> GetListFromCsv(string filePath) {
        TextFieldParser reader = new(filePath) {
            HasFieldsEnclosedInQuotes = true,
            Delimiters = new string[] { "," },
            CommentTokens = new string[] { "#" },
            TrimWhiteSpace = false,
            TextFieldType = FieldType.Delimited,
        };

        // consume header
        string[]? headerBlock = reader.ReadFields();

        List<VTuberId> rList = new();
        while (!reader.EndOfData) {
            string[]? entryBlock = reader.ReadFields();
            if (entryBlock is null || entryBlock.Length != 1) {
                continue;
            }

            rList.Add(new VTuberId(entryBlock[0]));
        }

        return rList;
    }

    public static Dictionary<string, string> GetDictFromCsv(string filePath) {
        TextFieldParser reader = new(filePath) {
            HasFieldsEnclosedInQuotes = true,
            Delimiters = new string[] { "," },
            CommentTokens = new string[] { "#" },
            TrimWhiteSpace = false,
            TextFieldType = FieldType.Delimited,
        };

        // consume header
        string[]? headerBlock = reader.ReadFields();

        Dictionary<string, string> rDict = new();
        while (!reader.EndOfData) {
            string[]? entryBlock = reader.ReadFields();
            if (entryBlock is null || entryBlock.Length != 2) {
                continue;
            }

            rDict.Add(entryBlock[0], entryBlock[1]);
        }

        return rDict;
    }

    public static TopVideosList GetTopVideoList(string filePath) {
        // CSV Format:
        // Display Name,View Count,Title,Publish Time,URL,Thumbnail URL
        // 璐洛洛,39127,【原神研究室】五郎全分析🐶▸提供珍貴岩元素暴傷，只為岩系隊伍輔助的忠犬！聖遺物/命座建議/天賦/武器/組隊搭配 ▹璐洛洛◃,2021-12-21T13:30:15Z,https://www.youtube.com/watch?v=NOHX-uAJ2Xg,https://i.ytimg.com/vi/NOHX-uAJ2Xg/default.jpg
        // 杏仁ミル,35115,跟壞朋友們迎接2022年!!!!!!!,2021 - 12 - 31T18: 58:28Z,https://www.youtube.com/watch?v=gMyV3wvn4bg,https://i.ytimg.com/vi/gMyV3wvn4bg/default.jpg

        TextFieldParser reader = new(filePath) {
            HasFieldsEnclosedInQuotes = true,
            Delimiters = new string[] { "," },
            TrimWhiteSpace = false,
            TextFieldType = FieldType.Delimited,
        };

        // consume header
        string[]? headerBlock = reader.ReadFields();
        if (headerBlock is null || headerBlock.Length != 6) {
            return new();
        }

        TopVideosList rList = new();

        while (!reader.EndOfData) {
            string[]? entryBlock = reader.ReadFields();

            if (entryBlock is not null)
                rList.Insert(entryBlock);
        }

        return rList;
    }
}
