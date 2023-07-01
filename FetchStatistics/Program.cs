using Common.Types;
using Common.Utils;
using FetchStatistics;
using log4net;

ILog log = LogManager.GetLogger("");

try {
    mainProcess();
} catch (Exception e) {
    log.Error("Unhandled exception");
    log.Error(e.Message, e);
}

void mainProcess() {
    string[] defaultArgs = new string[] {
        "./DATA/YOUTUBE_API_KEY",
        "./DATA/TWITCH_CLIENT_ID",
        "./DATA/TWITCH_SECRET",
        "./DATA/TW_VTUBER_TRACK_LIST.csv",
        "./DATA/EXCLUDE_LIST.csv",
        ".",
        FetchType.All.ToString(),
    };

    // this code block is only used to print read args
    {
        string youTubeApiKeyPath = args.Length >= 1 ? args[0] : defaultArgs[0];
        string twitchClientIdPath = args.Length >= 2 ? args[1] : defaultArgs[1];
        string twitchSecretPath = args.Length >= 3 ? args[2] : defaultArgs[2];
        string trackListPath = args.Length >= 4 ? args[3] : defaultArgs[3];
        string excludeListPath = args.Length >= 5 ? args[4] : defaultArgs[4];
        string savePath = args.Length >= 6 ? args[5] : defaultArgs[5];
        string fetchType = args.Length >= 7 ? args[6] : defaultArgs[6];

        log.Info("Configuration:");
        log.Info($"youTubeApiKeyPath: {youTubeApiKeyPath}");
        log.Info($"twitchClientIdPath: {twitchClientIdPath}");
        log.Info($"twitchSecretPath: {twitchSecretPath}");
        log.Info($"trackListPath: {trackListPath}");
        log.Info($"excludeListPath: {excludeListPath}");
        log.Info($"savePath: {savePath}");
        log.Info($"fetchType: {fetchType}");
    }

    // initialize constant values
    Config CONFIG = new(filePaths: args, defaultArgs: defaultArgs);
    DateTimeOffset CURRENT_TIME = DateTimeOffset.Now;

    // create statistics fetcher
    FetchYouTubeStatistics.Fetcher youtubeDataFetcher = new(CONFIG.youTubeApiKey, CURRENT_TIME);
    FetchTwitchStatistics.Fetcher twitchDataFetcher = new(CONFIG.twitchCrenditial, CURRENT_TIME);

    TrackList trackList = createTrackList(CONFIG.excludeListPath, CONFIG.trackListPath);

    switch (CONFIG.fetchType) {
        case FetchType.All:
            fetchAllAndWrite(youtubeDataFetcher, twitchDataFetcher, trackList, CONFIG.savePath, CURRENT_TIME);
            break;
        case FetchType.TwitchLiveStreamOnly:
            fetchTwitchLiveStreamsAndWrite(twitchDataFetcher, trackList, CONFIG.savePath, CURRENT_TIME);
            break;
    }


    log.Info("End program");
}

TrackList createTrackList(string excludeListPath, string trackListPath) {
    List<string> excluedList = FileUtility.GetListFromCsv(excludeListPath);
    log.Info($"excluedList: {string.Join(",", excluedList)}");
    TrackList trackList = new(csvFilePath: trackListPath, lstExcludeId: excluedList, throwOnValidationFail: true);
    log.Info($"trackList.GetCount(): {trackList.GetCount()}");

    return trackList;
}

void fetchAllAndWrite(
    FetchYouTubeStatistics.Fetcher youtubeDataFetcher,
    FetchTwitchStatistics.Fetcher twitchDataFetcher,
    TrackList trackList,
    string savePath,
    DateTimeOffset currentTime) {
    log.Info("Start getting all YouTube statistics");
    (Dictionary<VTuberId, YouTubeStatistics> dictIdYouTubeStatistics, TopVideosList youTubeTopVideosList, LiveVideosList youTubeLiveVideosList) =
        fetchYouTubeStatistics(youtubeDataFetcher, trackList);
    log.Info("End getting all YouTube statistics");

    log.Info("Start getting Twitch statistics");
    (Dictionary<VTuberId, TwitchStatistics> dictIdTwitchStatistics, TopVideosList twitchTopVideosList, LiveVideosList twitchLiveVideosList) =
        fetchTwitchStatistics(twitchDataFetcher, trackList);
    log.Info("End getting Twitch statistics");

    List<VTuberStatistics> lstStatistics = mergeStatistics(dictIdYouTubeStatistics, dictIdTwitchStatistics);
    TopVideosList topVideosList = youTubeTopVideosList.Insert(twitchTopVideosList.GetSortedList());
    LiveVideosList liveVideosList = youTubeLiveVideosList.Insert(twitchLiveVideosList);

    log.Info("Start writing files");
    writeFiles(lstStatistics, topVideosList, liveVideosList, savePath, currentTime);
    log.Info("End writing files");
}

void fetchTwitchLiveStreamsAndWrite(
    FetchTwitchStatistics.Fetcher twitchDataFetcher,
    TrackList trackList,
    string savePath,
    DateTimeOffset currentTime) {
    log.Info("Start getting Twitch statistics");
    (Dictionary<VTuberId, TwitchStatistics> dictIdTwitchStatistics, TopVideosList twitchTopVideosList, LiveVideosList twitchLiveVideosList) =
        fetchTwitchStatistics(twitchDataFetcher, trackList);
    log.Info("End getting Twitch statistics");

    log.Info("Start writing files");
    WriteFiles.WriteLiveVideosListResult(twitchLiveVideosList, currentTime, savePath, fileNamePrefix: "twitch-livestreams");
    log.Info("End writing files");
}

(Dictionary<VTuberId, YouTubeStatistics>, TopVideosList, LiveVideosList) fetchYouTubeStatistics(FetchYouTubeStatistics.Fetcher youtubeDataFetcher, TrackList trackList) {
    List<string> lstYouTubeChannelId = trackList.GetYouTubeChannelIdList();
    log.Info($"Channel count: {lstYouTubeChannelId.Count}");

    Dictionary<VTuberId, YouTubeStatistics> rDict = new();
    TopVideosList rVideosList;
    LiveVideosList rLiveVideosList;

    (Dictionary<string, YouTubeStatistics> dictIdYouTubeStatistics, rVideosList, rLiveVideosList) = youtubeDataFetcher.GetAll(lstYouTubeChannelId);

    foreach (KeyValuePair<string, YouTubeStatistics> keyValue in dictIdYouTubeStatistics) {
        try {
            VTuberId vTuberId = new(trackList.GetIdByYouTubeChannelId(keyValue.Key));
            rDict.Add(vTuberId, keyValue.Value);
        } catch (Exception e) {
            log.Error($"Error while converting rDict");
            log.Error($"GetIdByYouTubeChannelId with input {keyValue.Key}");
            log.Error(e.Message, e);
        }
    }

    foreach (VideoInformation videoInfo in rVideosList) {
        try {
            videoInfo.Id = trackList.GetIdByYouTubeChannelId(videoInfo.Id);
        } catch (Exception e) {
            log.Error($"Error while converting rVideoList");
            log.Error($"GetIdByYouTubeChannelId with input {videoInfo.Id}");
            log.Error(e.Message, e);
        }
    }

    foreach (LiveVideoInformation videoInfo in rLiveVideosList) {
        try {
            videoInfo.Id = trackList.GetIdByYouTubeChannelId(videoInfo.Id);
        } catch (Exception e) {
            log.Error($"Error while converting liveVideosList");
            log.Error($"GetIdByYouTubeChannelId with input {videoInfo.Id}");
            log.Error(e.Message, e);
        }
    }

    return (rDict, rVideosList, rLiveVideosList);
}

(Dictionary<VTuberId, TwitchStatistics>, TopVideosList, LiveVideosList) fetchTwitchStatistics(FetchTwitchStatistics.Fetcher twitchDataFetcher, TrackList trackList) {
    Dictionary<VTuberId, TwitchStatistics> rDict = new();
    TopVideosList rVideosList = new();
    LiveVideosList rLiveVideosList = new();

    foreach (VTuberData vtuber in trackList) {
        log.Info("Display Name: " + vtuber.DisplayName);
        log.Info("Twitch Channel ID: " + vtuber.TwitchChannelId);

        TwitchStatistics twitchStatistics = new();
        TopVideosList twitchTopVideoList = new();
        LiveVideosList twitchLiveVideosList = new();
        if (!string.IsNullOrEmpty(vtuber.TwitchChannelId)) {
            bool successful = twitchDataFetcher.GetAll(vtuber.TwitchChannelId, out twitchStatistics, out twitchTopVideoList, out twitchLiveVideosList);

            if (!successful) { continue; }
        }

        rDict.Add(new VTuberId(vtuber.Id), twitchStatistics);

        foreach (VideoInformation videoInfo in twitchTopVideoList) {
            try {
                videoInfo.Id = trackList.GetIdByTwitchChannelId(videoInfo.Id);
            } catch (Exception e) {
                log.Error($"Error while converting twitchTopVideoList");
                log.Error($"GetIdByTwitchChannelId with input {videoInfo.Id}");
                log.Error(e.Message, e);
            }

            rVideosList.Insert(videoInfo);
        }

        foreach (LiveVideoInformation videoInfo in twitchLiveVideosList) {
            try {
                videoInfo.Id = trackList.GetIdByTwitchChannelId(videoInfo.Id);
            } catch (Exception e) {
                log.Error($"Error while converting twitchTopVideoList");
                log.Error($"GetIdByTwitchChannelId with input {videoInfo.Id}");
                log.Error(e.Message, e);
            }

            rLiveVideosList.Add(videoInfo);
        }
    }

    return (rDict, rVideosList, rLiveVideosList);
}

List<VTuberStatistics> mergeStatistics(Dictionary<VTuberId, YouTubeStatistics> dictIdYouTubeStatistics, Dictionary<VTuberId, TwitchStatistics> dictIdTwitchStatistics) {
    HashSet<VTuberId> unionKeySet = dictIdYouTubeStatistics.Keys.ToHashSet();
    unionKeySet.UnionWith(dictIdTwitchStatistics.Keys.ToHashSet());

    List<VTuberStatistics> rLst = new(unionKeySet.Count);
    foreach (VTuberId vTuberId in unionKeySet) {
        YouTubeStatistics youTubeStatistics = dictIdYouTubeStatistics.GetValueOrDefault(key: vTuberId, defaultValue: new YouTubeStatistics());
        TwitchStatistics twitchStatistics = dictIdTwitchStatistics.GetValueOrDefault(key: vTuberId, defaultValue: new TwitchStatistics());

        rLst.Add(new VTuberStatistics(vTuberId.value, youTubeStatistics, twitchStatistics));
    }

    return rLst;
}

void writeFiles(List<VTuberStatistics> lstStatistics, TopVideosList topVideoList, LiveVideosList liveVideosList, string savePath, DateTimeOffset currentTime) {
    log.Info($"currentTime: {currentTime}");
    WriteFiles.WriteResult(lstStatistics, currentTime, savePath);
    WriteFiles.WriteTopVideosListResult(topVideoList, currentTime, savePath);
    WriteFiles.WriteLiveVideosListResult(liveVideosList, currentTime, savePath);
}
