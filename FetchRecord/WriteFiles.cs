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
            "(V5)VTuber ID," +
            "YouTube Subscriber Count," +
            "YouTube View Count," +
            "YouTube Recent Total Median View Count," +
            "YouTube Recent Total Popularity," +
            "YouTube Recent Total Highest View Count," +
            "YouTube Recent Total Highest Viewed URL," +
            "YouTube Recent Live Stream Median View Count," +
            "YouTube Recent Live Stream Popularity," +
            "YouTube Recent Live Stream Highest View Count," +
            "YouTube Recent Live Stream Highest Viewed URL," +
            "YouTube Recent Video Median View Count," +
            "YouTube Recent Video Popularity," +
            "YouTube Recent Video Highest View Count," +
            "YouTube Recent Video Highest Viewed URL," +
            "Twitch Follower Count," +
            "Twitch Recent Median View Count," +
            "Twitch Recent Popularity," +
            "Twitch Recent Highest View Count," +
            "Twitch Recent Highest Viewed URL\n");
        foreach (VTuberRecord record in lstVTuberRecord.OrderByDescending(e => e.YouTube.Basic.SubscriberCount)) {
            recordFile.Write(record.VTuberId.Value);
            recordFile.Write(',');

            recordFile.Write(record.YouTube.Basic.SubscriberCount);
            recordFile.Write(',');
            recordFile.Write(record.YouTube.Basic.ViewCount);
            recordFile.Write(',');
            recordFile.Write(record.YouTube.Recent.Total.MedialViewCount);
            recordFile.Write(',');
            recordFile.Write(record.YouTube.Recent.Total.Popularity);
            recordFile.Write(',');
            recordFile.Write(record.YouTube.Recent.Total.HighestViewCount);
            recordFile.Write(',');
            recordFile.Write(record.YouTube.Recent.Total.HighestViewdUrl);
            recordFile.Write(',');
            recordFile.Write(record.YouTube.Recent.LiveStream.MedialViewCount);
            recordFile.Write(',');
            recordFile.Write(record.YouTube.Recent.LiveStream.Popularity);
            recordFile.Write(',');
            recordFile.Write(record.YouTube.Recent.LiveStream.HighestViewCount);
            recordFile.Write(',');
            recordFile.Write(record.YouTube.Recent.LiveStream.HighestViewdUrl);
            recordFile.Write(',');
            recordFile.Write(record.YouTube.Recent.Video.MedialViewCount);
            recordFile.Write(',');
            recordFile.Write(record.YouTube.Recent.Video.Popularity);
            recordFile.Write(',');
            recordFile.Write(record.YouTube.Recent.Video.HighestViewCount);
            recordFile.Write(',');
            recordFile.Write(record.YouTube.Recent.Video.HighestViewdUrl);
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
