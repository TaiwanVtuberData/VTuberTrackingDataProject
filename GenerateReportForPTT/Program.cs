using GenerateReportForPTT;
using System.Globalization;

string commandLine = GetStringFromFile(args[0]);
string[] command = commandLine.Split(' ');

string columnHeader = command[2];
int maxColumnLength = int.Parse(command[3]);
bool sortByIncreasePercentage = int.Parse(command[4]) == 1;
bool onlyShowValueChanges = int.Parse(command[5]) == 1;
bool accountForLessThanAWeek = int.Parse(command[6]) == 1;

ChannelTable channelTable = new(valueHeader: columnHeader, sortByIncreasePercentage: sortByIncreasePercentage, onlyShowValueChanges: onlyShowValueChanges);

StreamReader file = new(command[0]);
string? line = file.ReadLine();
if (line == null) {
    return;
}

List<DateTime> dateTimeList = ParseDateTimeList(line);
int latestIndex = dateTimeList.Count - 1;
int lastWeekIndex = FindClosestIndex(dateTimeList, dateTimeList.Last(), new TimeSpan(days: 7, hours: 0, minutes: 0, seconds: 0));
int lastMonthIndex = FindClosestIndex(dateTimeList, dateTimeList.Last(), new TimeSpan(days: 28, hours: 0, minutes: 0, seconds: 0));

while ((line = file.ReadLine()) != null) {
    List<string> stringBlock = line.Split(',').ToList();
    string channelName = stringBlock[0];

    stringBlock = stringBlock.Skip(1).ToList();
    if (stringBlock.Count != dateTimeList.Count)
        throw new Exception("Data count doesn't match.");

    int thisLastWeekIndex = lastWeekIndex;
    bool isLesserThanLastWeek = false;
    if (accountForLessThanAWeek) {
        for (thisLastWeekIndex = lastWeekIndex; thisLastWeekIndex < stringBlock.Count; thisLastWeekIndex++) {
            if (stringBlock[thisLastWeekIndex] != "")
                break;
        }
        thisLastWeekIndex = Math.Min(thisLastWeekIndex, latestIndex);

        if (stringBlock[lastWeekIndex] == "")
            isLesserThanLastWeek = true;
    }

    channelTable.AddChannel(channelName, stringBlock[latestIndex], stringBlock[thisLastWeekIndex], stringBlock[lastMonthIndex], isLesserThanLastWeek);
}

file.Close();

StreamWriter writer = new(command[1]);
writer.Write(channelTable.ToString(maxColumnLength));
writer.Close();

static string GetStringFromFile(string filePath) {
    string? line;

    // Read the file and display it line by line.  
    using StreamReader file = new(filePath);
    if ((line = file.ReadLine()) != null) {
        file.Close();
        return line;
    }

    throw new Exception("Could not retrieve file path.");
}

static List<DateTime> ParseDateTimeList(string data) {
    string[] stringBlock = data.Split(',');

    List<DateTime> ans = new();
    foreach (string str in stringBlock) {
        if (DateTime.TryParseExact(str, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDateTime)) {
            ans.Add(parsedDateTime);
        }
    }

    return ans;
}

static int FindClosestIndex(List<DateTime> dateTimes, DateTime target, TimeSpan timeSpan) {
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