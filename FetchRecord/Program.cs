﻿using System.Collections.Immutable;
using Common.Types;
using Common.Types.Basic;
using Common.Utils;
using FetchRecord;
using log4net;

ILog log = LogManager.GetLogger("");

YouTubeRecord DEFAULT_YOUTUBE_RECORD =
    new(
        Basic: new(SubscriberCount: 0, ViewCount: 0),
        Recent: new(
            Total: new(MedialViewCount: 0, Popularity: 0, HighestViewCount: 0, HighestViewdUrl: ""),
            Livestream: new(
                MedialViewCount: 0,
                Popularity: 0,
                HighestViewCount: 0,
                HighestViewdUrl: ""
            ),
            Video: new(MedialViewCount: 0, Popularity: 0, HighestViewCount: 0, HighestViewdUrl: "")
        )
    );

log.Info("Start program");
try
{
    mainProcess();
}
catch (Exception e)
{
    log.Error("Unhandled exception");
    log.Error(e.Message, e);
}
log.Info("End program");

void mainProcess()
{
    // initialize constant values
    Config CONFIG = new();
    DateTimeOffset CURRENT_TIME = DateTimeOffset.Now;

    // create record fetcher
    FetchYouTubeStatistics.Fetcher youtubeDataFetcher = new(CONFIG.YouTubeApiKey, CURRENT_TIME);
    FetchTwitchStatistics.Fetcher twitchDataFetcher = new(CONFIG.TwitchCredential, CURRENT_TIME);

    TrackList trackList = createTrackList(
        CONFIG.ExcludeListPath,
        CONFIG.TrackListPath,
        CONFIG.IgnoreType
    );

    switch (CONFIG.FetchType)
    {
        case FetchType.All:
            fetchAllAndWrite(
                youtubeDataFetcher,
                twitchDataFetcher,
                trackList,
                CONFIG.SavePath,
                CURRENT_TIME
            );
            break;
        case FetchType.TwitchLivestreamOnly:
            fetchTwitchLivestreamsAndWrite(
                twitchDataFetcher,
                trackList,
                CONFIG.SavePath,
                CURRENT_TIME
            );
            break;
    }
}

TrackList createTrackList(string excludeListPath, string trackListPath, IgnoreType ignoreType)
{
    List<VTuberId> excludeList = FileUtility.GetListFromCsv(excludeListPath);
    log.Info($"excludeList from {excludeListPath}: {excludeList}");

    bool ignoreGraduated = ignoreType == IgnoreType.Graduated;

    TrackList trackList =
        new(
            csvFilePath: trackListPath,
            lstExcludeId: excludeList,
            ignoreGraduated: ignoreGraduated,
            throwOnValidationFail: true
        );
    log.Info($"trackList.GetCount(): {trackList.GetCount()}");

    return trackList;
}

void fetchAllAndWrite(
    FetchYouTubeStatistics.Fetcher youtubeDataFetcher,
    FetchTwitchStatistics.Fetcher twitchDataFetcher,
    TrackList trackList,
    string savePath,
    DateTimeOffset currentTime
)
{
    log.Info("Start getting all YouTube record");
    (
        ImmutableDictionary<VTuberId, YouTubeRecord> dictYouTubeRecord,
        TopVideosList youTubeTopVideosList,
        LiveVideosList youTubeLiveVideosList
    ) = fetchYouTubeRecord(youtubeDataFetcher, trackList);
    log.Info("End getting all YouTube record");

    log.Info("Start getting Twitch record");
    (
        Dictionary<VTuberId, TwitchStatistics> dictTwitchRecord,
        TopVideosList twitchTopVideosList,
        LiveVideosList twitchLiveVideosList
    ) = fetchTwitchRecord(twitchDataFetcher, trackList);
    log.Info("End getting Twitch record");

    ImmutableList<VTuberRecord> lstRecord = mergeRecord(
        dictYouTubeRecord,
        dictTwitchRecord.ToImmutableDictionary()
    );
    TopVideosList topVideosList = youTubeTopVideosList.Insert(twitchTopVideosList.GetSortedList());
    LiveVideosList liveVideosList = youTubeLiveVideosList.Insert(twitchLiveVideosList);

    log.Info("Start writing files");
    writeFiles(lstRecord, topVideosList, liveVideosList, savePath);
    log.Info("End writing files");
}

void fetchTwitchLivestreamsAndWrite(
    FetchTwitchStatistics.Fetcher twitchDataFetcher,
    TrackList trackList,
    string savePath,
    DateTimeOffset currentTime
)
{
    log.Info("Start getting Twitch record");
    (
        Dictionary<VTuberId, TwitchStatistics> dictTwitchRecord,
        TopVideosList twitchTopVideosList,
        LiveVideosList twitchLiveVideosList
    ) = fetchTwitchRecord(twitchDataFetcher, trackList);
    log.Info("End getting Twitch record");

    log.Info("Start writing files");
    WriteFiles.WriteLiveVideosListResult(
        twitchLiveVideosList,
        currentTime,
        savePath,
        fileNamePrefix: "twitch-livestreams"
    );
    log.Info("End writing files");
}

(ImmutableDictionary<VTuberId, YouTubeRecord>, TopVideosList, LiveVideosList) fetchYouTubeRecord(
    FetchYouTubeStatistics.Fetcher youtubeDataFetcher,
    TrackList trackList
)
{
    ImmutableList<YouTubeChannelId> lstYouTubeChannelId = trackList
        .GetYouTubeChannelIdList()
        .Map(e => new YouTubeChannelId(e))
        .ToImmutableList();
    log.Info($"Channel count: {lstYouTubeChannelId.Count}");

    Dictionary<VTuberId, YouTubeRecord> rDict = [];
    TopVideosList rVideosList;
    LiveVideosList rLiveVideosList;

    (
        ImmutableDictionary<YouTubeChannelId, YouTubeRecord> dictYouTubeRecord,
        rVideosList,
        rLiveVideosList
    ) = youtubeDataFetcher.GetAll(lstYouTubeChannelId);

    foreach (KeyValuePair<YouTubeChannelId, YouTubeRecord> keyValue in dictYouTubeRecord)
    {
        YouTubeChannelId youTubeChannelId = keyValue.Key;
        YouTubeRecord youTubeRecord = keyValue.Value;

        try
        {
            VTuberId vTuberId = trackList.GetIdByYouTubeChannelId(youTubeChannelId.Value);
            rDict.Add(vTuberId, youTubeRecord);
        }
        catch (Exception e)
        {
            log.Error($"Error while converting rDict");
            log.Error($"GetIdByYouTubeChannelId with input {keyValue.Key}");
            log.Error(e.Message, e);
        }
    }

    foreach (VideoInformation videoInfo in rVideosList)
    {
        try
        {
            videoInfo.Id = trackList.GetIdByYouTubeChannelId(videoInfo.Id.Value);
        }
        catch (Exception e)
        {
            log.Error($"Error while converting rVideoList");
            log.Error($"GetIdByYouTubeChannelId with input {videoInfo.Id}");
            log.Error(e.Message, e);
        }
    }

    foreach (LiveVideoInformation videoInfo in rLiveVideosList)
    {
        try
        {
            videoInfo.Id = trackList.GetIdByYouTubeChannelId(videoInfo.Id.Value);
        }
        catch (Exception e)
        {
            log.Error($"Error while converting liveVideosList");
            log.Error($"GetIdByYouTubeChannelId with input {videoInfo.Id}");
            log.Error(e.Message, e);
        }
    }

    return (rDict.ToImmutableDictionary(), rVideosList, rLiveVideosList);
}

(Dictionary<VTuberId, TwitchStatistics>, TopVideosList, LiveVideosList) fetchTwitchRecord(
    FetchTwitchStatistics.Fetcher twitchDataFetcher,
    TrackList trackList
)
{
    Dictionary<VTuberId, TwitchStatistics> rDict = [];
    TopVideosList rVideosList = new();
    LiveVideosList rLiveVideosList = [];

    Dictionary<string, TwitchStatistics> statisticDict = [];
    TopVideosList twitchTopVideoList = new();
    LiveVideosList twitchLiveVideosList = [];
    twitchDataFetcher.GetAll(
        trackList.GetTwitchChannelIdList().ToHashSet(),
        out statisticDict,
        out twitchTopVideoList,
        out twitchLiveVideosList
    );

    foreach (KeyValuePair<string, TwitchStatistics> keyValuePair in statisticDict)
    {
        string twitchChannelId = keyValuePair.Key;
        TwitchStatistics twitchStatistics = keyValuePair.Value;

        try
        {
            VTuberId vTuberId = trackList.GetIdByTwitchChannelId(twitchChannelId);
            rDict.Add(vTuberId, twitchStatistics);
        }
        catch (Exception e)
        {
            log.Error($"Error while converting Dictionary<VTuberId, TwitchStatistics>");
            log.Error($"GetIdByTwitchChannelId with input {twitchChannelId}");
            log.Error(e.Message, e);
        }
    }

    foreach (VideoInformation videoInfo in twitchTopVideoList)
    {
        try
        {
            videoInfo.Id = trackList.GetIdByTwitchChannelId(videoInfo.Id.Value);
            rVideosList.Insert(videoInfo);
        }
        catch (Exception e)
        {
            log.Error($"Error while converting twitchTopVideoList");
            log.Error($"GetIdByTwitchChannelId with input {videoInfo.Id}");
            log.Error(e.Message, e);
        }
    }

    foreach (LiveVideoInformation videoInfo in twitchLiveVideosList)
    {
        try
        {
            videoInfo.Id = trackList.GetIdByTwitchChannelId(videoInfo.Id.Value);
            rLiveVideosList.Add(videoInfo);
        }
        catch (Exception e)
        {
            log.Error($"Error while converting twitchLiveVideosList");
            log.Error($"GetIdByTwitchChannelId with input {videoInfo.Id}");
            log.Error(e.Message, e);
        }
    }

    return (rDict, rVideosList, rLiveVideosList);
}

ImmutableList<VTuberRecord> mergeRecord(
    IImmutableDictionary<VTuberId, YouTubeRecord> dictYouTubeRecord,
    IImmutableDictionary<VTuberId, TwitchStatistics> dictTwitchRecord
)
{
    HashSet<VTuberId> unionKeySet = dictYouTubeRecord.Keys.ToHashSet();
    unionKeySet.UnionWith(dictTwitchRecord.Keys.ToHashSet());

    List<VTuberRecord> rLst = new(unionKeySet.Count);
    foreach (VTuberId vTuberId in unionKeySet)
    {
        YouTubeRecord youTubeRecord = dictYouTubeRecord.GetValueOrDefault(
            key: vTuberId,
            defaultValue: DEFAULT_YOUTUBE_RECORD
        );
        TwitchStatistics twitchRecord = dictTwitchRecord.GetValueOrDefault(
            key: vTuberId,
            defaultValue: new TwitchStatistics()
        );

        rLst.Add(new VTuberRecord(vTuberId, youTubeRecord, twitchRecord));
    }

    return rLst.ToImmutableList();
}

void writeFiles(
    ImmutableList<VTuberRecord> lstRecord,
    TopVideosList topVideoList,
    LiveVideosList liveVideosList,
    string savePath
)
{
    DateTimeOffset currentDateTime = DateTimeOffset.Now;
    log.Info($"currentDateTime: {currentDateTime}");
    WriteFiles.WriteResult(lstRecord, currentDateTime, savePath);
    WriteFiles.WriteTopVideosListResult(topVideoList, currentDateTime, savePath);
    WriteFiles.WriteLiveVideosListResult(liveVideosList, currentDateTime, savePath);
}
