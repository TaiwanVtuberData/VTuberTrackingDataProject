using Common.Types;
using Common.Types.Basic;
using Common.Utils;
using GenerateRecordList.Types;

namespace GenerateRecordList.Utils;

public class MiscUtils
{
    public static string SetTwitchThumbnailUrlSize(string str, int width, int height)
    {
        if (!str.Contains("%{width}") || !str.Contains("%{height}"))
            return str;

        return str.Replace("%{width}", width.ToString()).Replace("%{height}", height.ToString());
    }

    public static string SetTwitchLivestreamThumbnailUrlSize(string str, int width, int height)
    {
        if (!str.Contains("{width}") || !str.Contains("{height}"))
            return str;

        return str.Replace("{width}", width.ToString()).Replace("{height}", height.ToString());
    }

    public static string ToIso8601UtcString(DateTimeOffset dateTime)
    {
        return dateTime.ToUniversalTime().ToString("o");
    }

    public static void FillRecord(
        ref DictionaryRecord dictRecord,
        TrackList trackList,
        string recordDir,
        DateTime targetDate,
        int recentDays
    )
    {
        List<Tuple<FileInfo, DateTimeOffset>> csvFileList = FileUtility.GetFileInfoDateTimeList(
            parentDirectory: recordDir,
            prefix: "record",
            targetTime: targetDate,
            recentDays: recentDays
        );

        foreach (Tuple<FileInfo, DateTimeOffset> fileInfoDateTime in csvFileList)
        {
            Dictionary<VTuberId, VTuberStatistics> dictStatistics =
                CsvUtility.ReadStatisticsDictionary(fileInfoDateTime.Item1.FullName);

            if (dictStatistics.Count >= trackList.GetCount() * 0.5)
            {
                dictRecord.AppendStatistic(fileInfoDateTime.Item2, dictStatistics);
                dictRecord.AppendBasicData(fileInfoDateTime.Item2, dictStatistics);
            }
        }
    }

    public static void FillRecordOnlyNecessary(
        ref DictionaryRecord dictRecord,
        string recordDir,
        DateTimeOffset targetTime,
        int recentDays
    )
    {
        List<Tuple<FileInfo, DateTimeOffset>> csvFileList = FileUtility.GetFileInfoDateTimeList(
            parentDirectory: recordDir,
            prefix: "record",
            targetTime: targetTime,
            recentDays: recentDays
        );

        foreach (Tuple<FileInfo, DateTimeOffset> fileInfoDateTime in csvFileList)
        {
            if (fileInfoDateTime.Item2 == targetTime)
            {
                Dictionary<VTuberId, VTuberStatistics> dictStatistics =
                    CsvUtility.ReadStatisticsDictionary(fileInfoDateTime.Item1.FullName);

                dictRecord.AppendStatistic(fileInfoDateTime.Item2, dictStatistics);
                dictRecord.AppendBasicData(fileInfoDateTime.Item2, dictStatistics);
            }

            TimeSpan duration = fileInfoDateTime.Item2 - targetTime;
            if (
                TimeSpan.FromDays(-recentDays) < duration
                && duration < TimeSpan.FromDays(-recentDays + 15)
            )
            {
                Dictionary<VTuberId, VTuberStatistics> dictStatistics =
                    CsvUtility.ReadStatisticsDictionary(fileInfoDateTime.Item1.FullName);

                dictRecord.AppendStatistic(fileInfoDateTime.Item2, dictStatistics);
                dictRecord.AppendBasicData(fileInfoDateTime.Item2, dictStatistics);
            }
        }
    }

    public static void FillBasicData(
        ref DictionaryRecord dictRecord,
        TrackList trackList,
        string basicDataDir,
        DateTime targetDate,
        int recentDays
    )
    {
        List<Tuple<FileInfo, DateTimeOffset>> csvFileList = FileUtility.GetFileInfoDateTimeList(
            parentDirectory: basicDataDir,
            prefix: "basic-data",
            targetTime: targetDate,
            recentDays: recentDays
        );

        foreach (Tuple<FileInfo, DateTimeOffset> fileInfoDateTime in csvFileList)
        {
            Dictionary<VTuberId, VTuberBasicData> dictBasicData = VTuberBasicData.ReadFromCsv(
                fileInfoDateTime.Item1.FullName
            );

            if (dictBasicData.Count >= trackList.GetCount() * 0.5)
            {
                dictRecord.AppendBasicData(fileInfoDateTime.Item2, dictBasicData);
            }
        }
    }

    public static void FillBasicDataOnlyNecessary(
        ref DictionaryRecord dictRecord,
        string basicDataDir,
        DateTimeOffset targetTime,
        int recentDays
    )
    {
        List<Tuple<FileInfo, DateTimeOffset>> csvFileList = FileUtility.GetFileInfoDateTimeList(
            parentDirectory: basicDataDir,
            prefix: "basic-data",
            targetTime: targetTime,
            recentDays: recentDays
        );

        foreach (Tuple<FileInfo, DateTimeOffset> fileInfoDateTime in csvFileList)
        {
            if (fileInfoDateTime.Item2 == targetTime)
            {
                Dictionary<VTuberId, VTuberBasicData> dictBasicData = VTuberBasicData.ReadFromCsv(
                    fileInfoDateTime.Item1.FullName
                );

                dictRecord.AppendBasicData(fileInfoDateTime.Item2, dictBasicData);
            }

            TimeSpan duration = fileInfoDateTime.Item2 - targetTime;
            if (
                TimeSpan.FromDays(-recentDays) < duration
                && duration < TimeSpan.FromDays(-recentDays + 15)
            )
            {
                Dictionary<VTuberId, VTuberBasicData> dictBasicData = VTuberBasicData.ReadFromCsv(
                    fileInfoDateTime.Item1.FullName
                );

                dictRecord.AppendBasicData(fileInfoDateTime.Item2, dictBasicData);
            }
        }
    }
}
