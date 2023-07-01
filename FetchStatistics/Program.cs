using Common.Types;
using Common.Utils;
using CsvHelper;
using FetchStatistics;
using log4net;
using System.Globalization;

ILog log = LogManager.GetLogger("");

try {
    string[] defaultArgs = new string[] {
        "./DATA/YOUTUBE_API_KEY",
        "./DATA/TWITCH_CLIENT_ID",
        "./DATA/TWITCH_SECRET",
        "./DATA/TW_VTUBER_TRACK_LIST.csv",
        "./DATA/EXCLUDE_LIST.csv",
        "."
    };

    // this code block is only used to print read args
    {
        string youTubeApiKeyPath = args.Length >= 1 ? args[0] : defaultArgs[0];
        string twitchClientIdPath = args.Length >= 2 ? args[1] : defaultArgs[1];
        string twitchSecretPath = args.Length >= 3 ? args[2] : defaultArgs[2];
        string trackListPath = args.Length >= 4 ? args[3] : defaultArgs[3];
        string excludeListPath = args.Length >= 5 ? args[4] : defaultArgs[4];
        string savePath = args.Length >= 6 ? args[5] : defaultArgs[5];

        log.Info("Configuration:");
        log.Info($"youTubeApiKeyPath: {youTubeApiKeyPath}");
        log.Info($"twitchClientIdPath: {twitchClientIdPath}");
        log.Info($"twitchSecretPath: {twitchSecretPath}");
        log.Info($"trackListPath: {trackListPath}");
        log.Info($"excludeListPath: {excludeListPath}");
        log.Info($"savePath: {savePath}");
    }

    // initialize constant values
    Config CONFIG = new(filePaths: args, defaultArgs: defaultArgs);
    DateTime CURRENT_TIME = DateTime.UtcNow;

    // create statistics fetcher
    FetchYouTubeStatistics.Fetcher youtubeDataFetcher = new(CONFIG.youTubeApiKey, CURRENT_TIME);
    FetchTwitchStatistics.Fetcher twitchDataFetcher = new(CONFIG.twitchCrenditial, CURRENT_TIME);

    TrackList trackList = createTrackList(CONFIG.excludeListPath, CONFIG.trackListPath);

    log.Info("Start getting all YouTube statistics");
    List<string> lstYouTubeChannelId = trackList.GetYouTubeChannelIdList();
    log.Info($"Channel count: {lstYouTubeChannelId.Count}");

    Dictionary<string, YouTubeStatistics> dictIdYouTubeStatistics;
    TopVideosList topVideoList;
    LiveVideosList liveVideosList;
    (dictIdYouTubeStatistics, topVideoList, liveVideosList) = youtubeDataFetcher.GetAll(lstYouTubeChannelId);

    foreach (VideoInformation videoInfo in topVideoList) {
        try {
            videoInfo.Id = trackList.GetIdByYouTubeChannelId(videoInfo.Id);
        } catch (Exception e) {
            log.Error($"Error while converting topVideoList");
            log.Error($"GetIdByYouTubeChannelId with input {videoInfo.Id}");
            log.Error(e.Message, e);
        }
    }

    foreach (LiveVideoInformation videoInfo in liveVideosList) {
        try {
            videoInfo.Id = trackList.GetIdByYouTubeChannelId(videoInfo.Id);
        } catch (Exception e) {
            log.Error($"Error while converting liveVideosList");
            log.Error($"GetIdByYouTubeChannelId with input {videoInfo.Id}");
            log.Error(e.Message, e);
        }
    }
    log.Info("End getting all YouTube statistics");

    log.Info("Start getting Twitch statistics");
    List<VTuberStatistics> lstStatistics = new();
    foreach (VTuberData vtuber in trackList) {
        log.Info("Display Name: " + vtuber.DisplayName);
        log.Info("Twitch Channel ID: " + vtuber.TwitchChannelId);

        TwitchStatistics twitchStatistics = new();
        TopVideosList twitchTopVideoList = new();
        LiveVideosList twitchLiveVideosList = new();
        if (!string.IsNullOrEmpty(vtuber.TwitchChannelId)) {
            bool successful = twitchDataFetcher.GetAll(vtuber.TwitchChannelId, out twitchStatistics, out twitchTopVideoList, out twitchLiveVideosList);
        }

        foreach (VideoInformation videoInfo in twitchTopVideoList) {
            try {
                videoInfo.Id = trackList.GetIdByTwitchChannelId(videoInfo.Id);
            } catch (Exception e) {
                log.Error($"Error while converting twitchTopVideoList");
                log.Error($"GetIdByTwitchChannelId with input {videoInfo.Id}");
                log.Error(e.Message, e);
            }

            topVideoList.Insert(videoInfo);
        }

        foreach (LiveVideoInformation videoInfo in twitchLiveVideosList) {
            try {
                videoInfo.Id = trackList.GetIdByTwitchChannelId(videoInfo.Id);
            } catch (Exception e) {
                log.Error($"Error while converting twitchTopVideoList");
                log.Error($"GetIdByTwitchChannelId with input {videoInfo.Id}");
                log.Error(e.Message, e);
            }

            liveVideosList.Add(videoInfo);
        }

        YouTubeStatistics youtubeStatistics = new();
        string YouTubeChannelId = trackList.GetYouTubeChannelId(vtuber.Id);
        if (dictIdYouTubeStatistics.ContainsKey(YouTubeChannelId)) {
            youtubeStatistics = dictIdYouTubeStatistics[YouTubeChannelId];
        }
        lstStatistics.Add(new VTuberStatistics(vtuber.Id, youtubeStatistics, twitchStatistics));
    }
    log.Info("End getting Twitch statistics");

    // save date as UTC+8 (Taiwan time zone)
    DateTime currentDateTime = DateTime.UtcNow.AddHours(8);
    log.Info($"currentDateTime: {currentDateTime}");
    WriteResult(lstStatistics, currentDateTime, CONFIG.savePath);
    WriteTopVideosListResult(topVideoList, currentDateTime, CONFIG.savePath);
    WriteLiveVideosListResult(liveVideosList, currentDateTime, CONFIG.savePath);
    log.Info("End program");
} catch (Exception e) {
    log.Error("Unhandled exception");
    log.Error(e.Message, e);
}

TrackList createTrackList(string excludeListPath, string trackListPath) {
    List<string> excluedList = FileUtility.GetListFromCsv(excludeListPath);
    log.Info($"excluedList: {string.Join(",", excluedList)}");
    TrackList trackList = new(csvFilePath: trackListPath, lstExcludeId: excluedList, throwOnValidationFail: true);
    log.Info($"trackList.GetCount(): {trackList.GetCount()}");

    return trackList;
}

void WriteResult(List<VTuberStatistics> vtuberStatisticsList, DateTime currentDateTime, string savePath) {
    // create monthly directory first
    string fileDir = $"{savePath}/{currentDateTime:yyyy-MM}";
    Directory.CreateDirectory(fileDir);

    string filePath = $"{fileDir}/record_{currentDateTime:yyyy-MM-dd-HH-mm-ss}.csv";
    log.Info($"Write statistics to : {filePath}");
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
    foreach (VTuberStatistics statistics in vtuberStatisticsList.OrderByDescending(p => p.YouTube.SubscriberCount)) {
        recordFile.Write(statistics.Id);
        recordFile.Write(',');

        recordFile.Write(statistics.YouTube.SubscriberCount);
        recordFile.Write(',');
        recordFile.Write(statistics.YouTube.ViewCount);
        recordFile.Write(',');
        recordFile.Write(statistics.YouTube.RecentMedianViewCount);
        recordFile.Write(',');
        recordFile.Write(statistics.YouTube.RecentPopularity);
        recordFile.Write(',');
        recordFile.Write(statistics.YouTube.RecentHighestViewCount);
        recordFile.Write(',');
        recordFile.Write(statistics.YouTube.HighestViewedVideoURL);
        recordFile.Write(',');

        recordFile.Write(statistics.Twitch.FollowerCount);
        recordFile.Write(',');
        recordFile.Write(statistics.Twitch.RecentMedianViewCount);
        recordFile.Write(',');
        recordFile.Write(statistics.Twitch.RecentPopularity);
        recordFile.Write(',');
        recordFile.Write(statistics.Twitch.RecentHighestViewCount);
        recordFile.Write(',');
        recordFile.Write(statistics.Twitch.HighestViewedVideoURL);
        recordFile.Write('\n');
    }

    recordFile.Close();
}

void WriteTopVideosListResult(TopVideosList topVideoList, DateTime currentDateTime, string savePath) {
    // create monthly directory first
    string fileDir = $"{savePath}/{currentDateTime:yyyy-MM}";
    Directory.CreateDirectory(fileDir);

    string filePath = $"{fileDir}/top-videos_{currentDateTime:yyyy-MM-dd-HH-mm-ss}.csv";
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

void WriteLiveVideosListResult(LiveVideosList liveVideos, DateTime currentDateTime, string savePath) {
    // create monthly directory first
    string fileDir = $"{savePath}/{currentDateTime:yyyy-MM}";
    Directory.CreateDirectory(fileDir);

    string filePath = $"{fileDir}/livestreams_{currentDateTime:yyyy-MM-dd-HH-mm-ss}.csv";
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
