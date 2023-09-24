using Common.Types;
using Common.Types.Basic;
using Common.Utils;
using FetchRecord;
using log4net;
using System.Collections.Immutable;

ILog log = LogManager.GetLogger("");

YouTubeRecord DEFAULT_YOUTUBE_RECORD = new(
    Basic: new(
        SubscriberCount: 0,
        ViewCount: 0
        ),
    Recent: new(
        Total: new(
            MedialViewCount: 0,
            Popularity: 0,
            HighestViewCount: 0,
            HighestViewdUrl: ""
            ),
        Livestream: new(
            MedialViewCount: 0,
            Popularity: 0,
            HighestViewCount: 0,
            HighestViewdUrl: ""
            ),
        Video: new(
            MedialViewCount: 0,
            Popularity: 0,
            HighestViewCount: 0,
            HighestViewdUrl: ""
            )
        )
    );

log.Info("Start program");
try {
    mainProcess();
} catch (Exception e) {
    log.Error("Unhandled exception");
    log.Error(e.Message, e);
}
log.Info("End program");

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

    // create record fetcher
    FetchYouTubeStatistics.Fetcher youtubeDataFetcher = new(CONFIG.YouTubeApiKey, CURRENT_TIME);
    FetchTwitchStatistics.Fetcher twitchDataFetcher = new(CONFIG.TwitchCrenditial, CURRENT_TIME);

    TrackList trackList = createTrackList(CONFIG.ExcludeListPath, CONFIG.TrackListPath);

    switch (CONFIG.FetchType) {
        case FetchType.All:
            fetchAllAndWrite(youtubeDataFetcher, twitchDataFetcher, trackList, CONFIG.SavePath, CURRENT_TIME);
            break;
        case FetchType.TwitchLivestreamOnly:
            fetchTwitchLivestreamsAndWrite(twitchDataFetcher, trackList, CONFIG.SavePath, CURRENT_TIME);
            break;
    }
}

TrackList createTrackList(string excludeListPath, string trackListPath) {
    List<VTuberId> excluedList = FileUtility.GetListFromCsv(excludeListPath);
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
    log.Info("Start getting all YouTube record");
    (ImmutableDictionary<VTuberId, YouTubeRecord> dictYouTubeRecord, TopVideosList youTubeTopVideosList, LiveVideosList youTubeLiveVideosList) =
        fetchYouTubeRecord(youtubeDataFetcher, trackList);
    log.Info("End getting all YouTube record");

    log.Info("Start getting Twitch record");
    (Dictionary<VTuberId, TwitchStatistics> dictTwitchRecord, TopVideosList twitchTopVideosList, LiveVideosList twitchLiveVideosList) =
        fetchTwitchRecord(twitchDataFetcher, trackList);
    log.Info("End getting Twitch record");

    ImmutableList<VTuberRecord> lstRecord = mergeRecord(dictYouTubeRecord, dictTwitchRecord.ToImmutableDictionary());
    TopVideosList topVideosList = youTubeTopVideosList.Insert(twitchTopVideosList.GetSortedList());
    LiveVideosList liveVideosList = youTubeLiveVideosList.Insert(twitchLiveVideosList);

    log.Info("Start writing files");
    writeFiles(lstRecord, topVideosList, liveVideosList, savePath, currentTime);
    log.Info("End writing files");
}

void fetchTwitchLivestreamsAndWrite(
    FetchTwitchStatistics.Fetcher twitchDataFetcher,
    TrackList trackList,
    string savePath,
    DateTimeOffset currentTime) {
    log.Info("Start getting Twitch record");
    (Dictionary<VTuberId, TwitchStatistics> dictTwitchRecord, TopVideosList twitchTopVideosList, LiveVideosList twitchLiveVideosList) =
        fetchTwitchRecord(twitchDataFetcher, trackList);
    log.Info("End getting Twitch record");

    log.Info("Start writing files");
    WriteFiles.WriteLiveVideosListResult(twitchLiveVideosList, currentTime, savePath, fileNamePrefix: "twitch-livestreams");
    log.Info("End writing files");
}

(ImmutableDictionary<VTuberId, YouTubeRecord>, TopVideosList, LiveVideosList) fetchYouTubeRecord(
    FetchYouTubeStatistics.Fetcher youtubeDataFetcher, TrackList trackList) {
    ImmutableList<YouTubeChannelId> lstYouTubeChannelId = trackList.GetYouTubeChannelIdList().Map(e => new YouTubeChannelId(e)).ToImmutableList();
    log.Info($"Channel count: {lstYouTubeChannelId.Count}");

    Dictionary<VTuberId, YouTubeRecord> rDict = new();
    TopVideosList rVideosList;
    LiveVideosList rLiveVideosList;

    (ImmutableDictionary<YouTubeChannelId, YouTubeRecord> dictYouTubeRecord, rVideosList, rLiveVideosList) =
        youtubeDataFetcher.GetAll(lstYouTubeChannelId);

    foreach (KeyValuePair<YouTubeChannelId, YouTubeRecord> keyValue in dictYouTubeRecord) {
        YouTubeChannelId youTubeChannelId = keyValue.Key;
        YouTubeRecord youTubeRecord = keyValue.Value;

        try {
            VTuberId vTuberId = trackList.GetIdByYouTubeChannelId(youTubeChannelId.Value);
            rDict.Add(vTuberId, youTubeRecord);
        } catch (Exception e) {
            log.Error($"Error while converting rDict");
            log.Error($"GetIdByYouTubeChannelId with input {keyValue.Key}");
            log.Error(e.Message, e);
        }
    }

    foreach (VideoInformation videoInfo in rVideosList) {
        try {
            videoInfo.Id = trackList.GetIdByYouTubeChannelId(videoInfo.Id.Value);
        } catch (Exception e) {
            log.Error($"Error while converting rVideoList");
            log.Error($"GetIdByYouTubeChannelId with input {videoInfo.Id}");
            log.Error(e.Message, e);
        }
    }

    foreach (LiveVideoInformation videoInfo in rLiveVideosList) {
        try {
            videoInfo.Id = trackList.GetIdByYouTubeChannelId(videoInfo.Id.Value);
        } catch (Exception e) {
            log.Error($"Error while converting liveVideosList");
            log.Error($"GetIdByYouTubeChannelId with input {videoInfo.Id}");
            log.Error(e.Message, e);
        }
    }

    return (rDict.ToImmutableDictionary(), rVideosList, rLiveVideosList);
}

(Dictionary<VTuberId, TwitchStatistics>, TopVideosList, LiveVideosList) fetchTwitchRecord(FetchTwitchStatistics.Fetcher twitchDataFetcher, TrackList trackList) {
    Dictionary<VTuberId, TwitchStatistics> rDict = new();
    TopVideosList rVideosList = new();
    LiveVideosList rLiveVideosList = new();

    foreach (VTuberData vtuber in trackList) {
        log.Info("Display Name: " + vtuber.DisplayName);
        log.Info("Twitch Channel ID: " + vtuber.TwitchChannelId);

        TwitchStatistics twitchRecord = new();
        TopVideosList twitchTopVideoList = new();
        LiveVideosList twitchLiveVideosList = new();
        if (!string.IsNullOrEmpty(vtuber.TwitchChannelId)) {
            bool successful = twitchDataFetcher.GetAll(vtuber.TwitchChannelId, out twitchRecord, out twitchTopVideoList, out twitchLiveVideosList);

            if (!successful) { continue; }
        }

        rDict.Add(vtuber.Id, twitchRecord);

        foreach (VideoInformation videoInfo in twitchTopVideoList) {
            try {
                videoInfo.Id = trackList.GetIdByTwitchChannelId(videoInfo.Id.Value);
            } catch (Exception e) {
                log.Error($"Error while converting twitchTopVideoList");
                log.Error($"GetIdByTwitchChannelId with input {videoInfo.Id}");
                log.Error(e.Message, e);
            }

            rVideosList.Insert(videoInfo);
        }

        foreach (LiveVideoInformation videoInfo in twitchLiveVideosList) {
            try {
                videoInfo.Id = trackList.GetIdByTwitchChannelId(videoInfo.Id.Value);
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

ImmutableList<VTuberRecord> mergeRecord(
    IImmutableDictionary<VTuberId, YouTubeRecord> dictYouTubeRecord,
    IImmutableDictionary<VTuberId, TwitchStatistics> dictTwitchRecord) {
    HashSet<VTuberId> unionKeySet = dictYouTubeRecord.Keys.ToHashSet();
    unionKeySet.UnionWith(dictTwitchRecord.Keys.ToHashSet());

    List<VTuberRecord> rLst = new(unionKeySet.Count);
    foreach (VTuberId vTuberId in unionKeySet) {
        YouTubeRecord youTubeRecord = dictYouTubeRecord.GetValueOrDefault(key: vTuberId, defaultValue: DEFAULT_YOUTUBE_RECORD);
        TwitchStatistics twitchRecord = dictTwitchRecord.GetValueOrDefault(key: vTuberId, defaultValue: new TwitchStatistics());

        rLst.Add(new VTuberRecord(vTuberId, youTubeRecord, twitchRecord));
    }

    return rLst.ToImmutableList();
}

void writeFiles(ImmutableList<VTuberRecord> lstRecord, TopVideosList topVideoList, LiveVideosList liveVideosList, string savePath, DateTimeOffset currentTime) {
    log.Info($"currentTime: {currentTime}");
    WriteFiles.WriteResult(lstRecord, currentTime, savePath);
    WriteFiles.WriteTopVideosListResult(topVideoList, currentTime, savePath);
    WriteFiles.WriteLiveVideosListResult(liveVideosList, currentTime, savePath);
}
