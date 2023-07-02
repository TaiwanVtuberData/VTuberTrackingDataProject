using Common.Types;
using CsvHelper;
using log4net;
using System.Collections.Immutable;
using System.Globalization;

namespace FetchStatistics;
internal class WriteFiles {
    private static readonly ILog log = LogManager.GetLogger(typeof(WriteFiles));

    public static void WriteResult(ImmutableList<VTuberRecord> lstVTuberRecord, DateTimeOffset currentTime, string savePath) {
        // create monthly directory first
        string fileDir = $"{savePath}/{currentTime:yyyy-MM}";
        Directory.CreateDirectory(fileDir);

        string filePath = $"{fileDir}/record_{currentTime:yyyy-MM-dd-HH-mm-ss}.csv";
        log.Info($"Write record to : {filePath}");
        using StreamWriter recordFile = new(filePath);
        recordFile.Write(
            "VTuber ID," +
            "YouTube Subscriber Count," +
            "YouTube View Count," +
            "YouTube Recent Median View Count," +
            "YouTube Recent Popularity," +
            "YouTube Recent Highest View Count," +
            "YouTube Recent Highest Viewed Video URL," +
            "Twitch Follower Count," +
            "Twitch Recent Median View Count," +
            "Twitch Recent Popularity," +
            "Twitch Recent Highest View Count," +
            "Twitch Recent Highest Viewed Video URL\n");
        foreach (VTuberRecord record in lstVTuberRecord.OrderByDescending(e => e.YouTube.Basic.SubscriberCount)) {
            recordFile.Write(record.VTuberId.Value);
            recordFile.Write(',');

            recordFile.Write(record.YouTube.Basic.SubscriberCount);
            recordFile.Write(',');
            recordFile.Write(record.YouTube.Basic.ViewCount);
            recordFile.Write(',');
            recordFile.Write(record.YouTube.RecentTotal.MedialViewCount);
            recordFile.Write(',');
            recordFile.Write(record.YouTube.RecentTotal.Popularity);
            recordFile.Write(',');
            recordFile.Write(record.YouTube.RecentTotal.HighestViewCount);
            recordFile.Write(',');
            recordFile.Write(record.YouTube.RecentTotal.HighestViewdUrl);
            recordFile.Write(',');

            recordFile.Write(record.Twitch.FollowerCount);
            recordFile.Write(',');
            recordFile.Write(record.Twitch.RecentMedianViewCount);
            recordFile.Write(',');
            recordFile.Write(record.Twitch.RecentPopularity);
            recordFile.Write(',');
            recordFile.Write(record.Twitch.RecentHighestViewCount);
            recordFile.Write(',');
            recordFile.Write(record.Twitch.HighestViewedVideoURL);
            recordFile.Write('\n');
        }

        recordFile.Close();
    }

    public static void WriteTopVideosListResult(TopVideosList topVideoList, DateTimeOffset currentTime, string savePath) {
        // create monthly directory first
        string fileDir = $"{savePath}/{currentTime:yyyy-MM}";
        Directory.CreateDirectory(fileDir);

        string filePath = $"{fileDir}/top-videos_{currentTime:yyyy-MM-dd-HH-mm-ss}.csv";
        log.Info($"Write top videos list to : {filePath}");
        using StreamWriter writer = new(filePath);
        using CsvWriter csv = new(writer, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap<VideoInformationMap>();

        csv.WriteHeader<VideoInformation>();

        foreach (VideoInformation videoInfo in topVideoList.GetSortedList().OrderByDescending(e => e.ViewCount)) {
            csv.NextRecord();
            csv.WriteRecord(videoInfo);
        }
    }

    public static void WriteLiveVideosListResult(LiveVideosList liveVideos, DateTimeOffset currentTime, string savePath, string fileNamePrefix = "livestreams") {
        // create monthly directory first
        string fileDir = $"{savePath}/{currentTime:yyyy-MM}";
        Directory.CreateDirectory(fileDir);

        string filePath = $"{fileDir}/{fileNamePrefix}_{currentTime:yyyy-MM-dd-HH-mm-ss}.csv";
        log.Info($"Write live videos list to : {filePath}");
        using StreamWriter writer = new(filePath);
        using CsvWriter csv = new(writer, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap<LiveVideoInformationMap>();

        csv.WriteHeader<LiveVideoInformation>();

        foreach (LiveVideoInformation videoInfo in liveVideos.OrderBy(e => e.PublishDateTime)) {
            csv.NextRecord();
            csv.WriteRecord(videoInfo);
        }
    }

}
