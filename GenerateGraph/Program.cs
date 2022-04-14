using Common.Types;
using Common.Utils;
using GenerateGraph;
using Microsoft.VisualBasic.FileIO;

int recentDays = -1;
if (args.Length == 1)
    recentDays = int.Parse(args[0]);

string csvDirectory = GetCsvDirectory("./CsvDirectory");
string trackListPath = Path.Combine(csvDirectory, "./DATA/TW_VTUBER_TRACK_LIST.csv");
TrackList trackList = new(csvFilePath: trackListPath, requiredLevel: 999, throwOnValidationFail: true);

WriteDateTimeStatistics(trackList, csvDirectory, recentDays, byGroup: false, "Individual");
WriteDateTimeStatistics(trackList, csvDirectory, recentDays, byGroup: true, "Group");

static string GetCsvDirectory(string filePath)
{
    string? line;

    // Read the file and display it line by line.  
    using StreamReader file = new(filePath);
    if ((line = file.ReadLine()) != null)
    {
        file.Close();
        return line;
    }

    throw new Exception("Could not retrieve Csv directory key.");
}

// ignore recentDays if it's lower than 0
static void WriteDateTimeStatistics(TrackList trackList, string recordDirectory, int recentDays, bool byGroup, string writePrefix)
{
    List<Tuple<FileInfo, DateTime>> csvFileList = FileUtility.GetFileInfoDateTimeList(recordDirectory, recentDays);

    StatisticsTable statisticsTable = new(trackList, requiredLevel: 999, byGroup);
    foreach (Tuple<FileInfo, DateTime> fileInfoDateTime in csvFileList)
    {
        Dictionary<string, VTuberStatistics> statisticsDictionary = GetStatisticsDictionaryFromRecordCSV(trackList, fileInfoDateTime.Item1.FullName, byGroup);

        bool shouldAdd = false;
        if (byGroup)
        {
            if (statisticsDictionary.Count >= trackList.GetGroupNameList().Count * 0.8)
            {
                shouldAdd = true;
            }
        }
        else
        {
            if (statisticsDictionary.Count >= trackList.GetCount() * 0.5)
            {
                shouldAdd = true;
            }
        }

        if (shouldAdd)
        {
            statisticsTable.AddRow(dateTime: fileInfoDateTime.Item2, statisticsDictionary);
        }
    }
    statisticsTable.FillEmptyValueByInterpolation();

    string[] names = {
                "YouTube.SubscriberCount",
                "YouTube.ViewCount",
                "YouTube.RecentMedianViewCount",
                "YouTube.RecentHighestViewCount",
                //"YouTube.SubscriberCountToMedianViewCount",
                "Twitch.FollowerCount",
                "Twitch.RecentMedianViewCount",
                "Twitch.RecentHighestViewCount",
                "Twitch.FollowerCountToMedianViewCount",
                "CombinedRecentMedianViewCount",
            };

    foreach (string name in names)
    {
        WriteIndividualRecordCSV(statisticsTable, name, null, writePrefix);
    }

    string[] namesConstriant = {
                "YouTube.SubscriberCountToMedianViewCount",
            };

    foreach (string name in namesConstriant)
    {
        WriteIndividualRecordCSV(statisticsTable, name, 2000m, writePrefix);
    }
}

static Dictionary<string, VTuberStatistics> GetStatisticsDictionaryFromRecordCSV(TrackList trackList, string filePath, bool byGroup)
{
    // CSV Format:
    // Name,SubscriberCount,ViewCount,MedianViewCount,HighestViewCount
    // 鳥羽樂奈,40600,1613960,9725,23248
    // 香草奈若,26900,1509583,15267,57825

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

    if (headerBlock is null)
        return new();

    VTuberStatistics.Version version = VTuberStatistics.GetVersionByHeaderLength(headerBlock.Length);
    if (version == VTuberStatistics.Version.Unknown)
        return new();

    Dictionary<string, VTuberStatistics> ans = new();
    int entryCount = 0;
    int groupEntryCount = 0;
    while (!reader.EndOfData)
    {
        string[]? entryBlock = reader.ReadFields();
        if (entryBlock is null)
            continue;

        entryCount++;

        string name = entryBlock[0];
        string displayName = trackList.GetDisplayName(name);

        if (byGroup == false)
        {
            ans.Add(displayName, new VTuberStatistics(entryBlock));
        }
        else
        {
            string groupName = trackList.GetGroupNameByName(displayName);
            if (groupName == "")
                continue;

            groupEntryCount++;

            if (!ans.ContainsKey(groupName))
                ans.Add(groupName, new VTuberStatistics(entryBlock));
            else
                ans[groupName].Add(entryBlock);
        }
    }

    if (byGroup)
    {
        if (groupEntryCount <= trackList.GetVtuberWithGroupCount() * 0.7)
            return new();
    }

    return ans;
}

static void WriteIndividualRecordCSV(StatisticsTable statisticsTable, string writeColumnName, decimal? subConstriant, string writePrefix)
{
    List<DateTime> dateList = statisticsTable.GetDateTimeList();
    dateList.Sort();
    DateTime minDate = dateList.Min();
    DateTime maxDate = dateList.Max();

    Dictionary<string, List<decimal>> statisticsDict = statisticsTable.GetStatisticDictByField(writeColumnName, subConstriant);

    using StreamWriter file = new(
        path: $"./{writePrefix}_{minDate:yyyy-MM-dd}_{maxDate:yyyy-MM-dd}_{writeColumnName}.csv",
        append: false,
        encoding: System.Text.Encoding.UTF8);
    // write the first column of format: Timestamp, 2021/2/4 20:44:12, 2021/2/4 20:46:45
    file.Write("Timestamp");
    foreach (DateTime dateTime in statisticsTable.GetDateTimeList())
    {
        file.Write($",{dateTime:yyyy/MM/dd HH:mm:ss}");
    }
    file.Write('\n');

    // then write the data by the order of the latest value
    foreach (KeyValuePair<string, List<decimal>> writtenValue in statisticsDict.OrderByDescending(p => p.Value.Last()))
    {
        string line = writtenValue.Key;
        List<decimal> copiedList = writtenValue.Value;

        foreach (decimal value in copiedList)
        {
            string stringToWrite = (value > 0m) ? value.ToString() : "";
            line += ',' + stringToWrite;
        }
        file.Write(line);
        file.Write('\n');
    }
    file.Close();
}