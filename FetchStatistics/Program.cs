﻿using Common.Types;
using Common.Utils;
using CsvHelper;
using System.Globalization;

string YouTubeApiKeyPath = args.Length >= 1 ? args[0] : "./DATA/YOUTUBE_API_KEY";
string TwitchClientIdPath = args.Length >= 2 ? args[1] : "./DATA/TWITCH_CLIENT_ID";
string TwitchSecretPath = args.Length >= 3 ? args[2] : "./DATA/TWITCH_SECRET";
string trackListPath = args.Length >= 4 ? args[3] : "./DATA/TW_VTUBER_TRACK_LIST.csv";
string excludeListPath = args.Length >= 5 ? args[4] : "./DATA/EXCLUDE_LIST.csv";
string savePath = args.Length >= 6 ? args[5] : ".";

Console.WriteLine("Configuration:");
Console.WriteLine(YouTubeApiKeyPath);
Console.WriteLine(TwitchClientIdPath);
Console.WriteLine(TwitchSecretPath);
Console.WriteLine(trackListPath);
Console.WriteLine(excludeListPath);
Console.WriteLine(savePath);

FetchYouTubeStatistics.Fetcher youtubeDataFetcher = new(FileUtility.GetSingleLineFromFile(YouTubeApiKeyPath));
FetchTwitchStatistics.Fetcher twitchDataFetcher = new(FileUtility.GetSingleLineFromFile(TwitchClientIdPath),
    FileUtility.GetSingleLineFromFile(TwitchSecretPath));

List<string> excluedList = FileUtility.GetListFromCsv(excludeListPath);
TrackList trackList = new(csvFilePath: trackListPath, lstExcludeId: excluedList, throwOnValidationFail: true);

List<string> lstYouTubeChannelId = trackList.GetYouTubeChannelIdList();
Console.WriteLine($"Get all YouTube statistics: {lstYouTubeChannelId.Count} channenls");

Dictionary<string, YouTubeStatistics> dictIdYouTubeStatistics;
TopVideosList topVideoList;
(dictIdYouTubeStatistics, topVideoList) = youtubeDataFetcher.GetAll(lstYouTubeChannelId);
foreach (VideoInformation videoInfo in topVideoList)
{
    try
    {
        videoInfo.Id = trackList.GetIdByYouTubeChannelId(videoInfo.Id);
    }
    catch
    {
    }
}

List<VTuberStatistics> lstStatistics = new();
foreach (VTuberData vtuber in trackList)
{
    Console.WriteLine("Display Name: " + vtuber.DisplayName);
    Console.WriteLine("Twitch Channel ID: " + vtuber.TwitchChannelId);

    TwitchStatistics twitchStatistics = new();
    TopVideosList twitchTopVideoList = new();
    if (!string.IsNullOrEmpty(vtuber.TwitchChannelId))
    {
        bool successful = twitchDataFetcher.GetAll(vtuber.TwitchChannelId, out twitchStatistics, out twitchTopVideoList);
    }
    foreach (VideoInformation videoInfo in twitchTopVideoList)
    {
        try
        {
            videoInfo.Id = trackList.GetIdByTwitchChannelId(videoInfo.Id);
        }
        catch
        {
        }

        topVideoList.Insert(videoInfo);
    }

    YouTubeStatistics youtubeStatistics = new();
    string YouTubeChannelId = trackList.GetYouTubeChannelId(vtuber.Id);
    if (dictIdYouTubeStatistics.ContainsKey(YouTubeChannelId))
    {
        youtubeStatistics = dictIdYouTubeStatistics[YouTubeChannelId];
    }
    lstStatistics.Add(new VTuberStatistics(vtuber.Id, youtubeStatistics, twitchStatistics));
}

// save date as UTC+8 (Taiwan time zone)
DateTime currentDateTime = DateTime.UtcNow.AddHours(8);
WriteResult(lstStatistics, currentDateTime, savePath);
WriteTopVideosListResult(topVideoList, currentDateTime, savePath);

static void WriteResult(List<VTuberStatistics> vtuberStatisticsList, DateTime currentDateTime, string savePath)
{
    // CSV Format:
    // Display Name,View Count,Title,Publish Time,URL,Thumbnail URL
    // 鳥羽樂奈,40000,1000000,https://www.youtube.com/watch?v=SLRESBPj2v8
    // 香草奈若,25000,1000000,https://www.youtube.com/watch?v=SLRESBPj2v8

    // create monthly directory first
    string fileDir = $"{savePath}/{currentDateTime:yyyy-MM}";
    Directory.CreateDirectory(fileDir);

    using StreamWriter recordFile = new($"{fileDir}/record_{currentDateTime:yyyy-MM-dd-HH-mm-ss}.csv");
    recordFile.Write(
        "VTuber ID," +
        "YouTube Subscriber Count," +
        "YouTube View Count," +
        "YouTube Recent Median View Count," +
        "YouTube Recent Highest View Count," +
        "YouTube Recent Highest Viewed Video URL," +
        "Twitch Follower Count," +
        "Twitch Recent Median View Count," +
        "Twitch Recent Highest View Count," +
        "Twitch Recent Highest Viewed Video URL\n");
    foreach (VTuberStatistics statistics in vtuberStatisticsList.OrderByDescending(p => p.YouTube.SubscriberCount))
    {
        recordFile.Write(statistics.Id);
        recordFile.Write(',');

        recordFile.Write(statistics.YouTube.SubscriberCount);
        recordFile.Write(',');
        recordFile.Write(statistics.YouTube.ViewCount);
        recordFile.Write(',');
        recordFile.Write(statistics.YouTube.RecentMedianViewCount);
        recordFile.Write(',');
        recordFile.Write(statistics.YouTube.RecentHighestViewCount);
        recordFile.Write(',');
        recordFile.Write(statistics.YouTube.HighestViewedVideoURL);
        recordFile.Write(',');

        recordFile.Write(statistics.Twitch.FollowerCount);
        recordFile.Write(',');
        recordFile.Write(statistics.Twitch.RecentMedianViewCount);
        recordFile.Write(',');
        recordFile.Write(statistics.Twitch.RecentHighestViewCount);
        recordFile.Write(',');
        recordFile.Write(statistics.Twitch.HighestViewedVideoURL);
        recordFile.Write('\n');
    }

    recordFile.Close();
}

static void WriteTopVideosListResult(TopVideosList topVideoList, DateTime currentDateTime, string savePath)
{
    // CSV Format:
    // Display Name,View Count,Title,Publish Time,URL,Thumbnail URL
    // 璐洛洛,39127,【原神研究室】五郎全分析🐶▸提供珍貴岩元素暴傷，只為岩系隊伍輔助的忠犬！聖遺物/命座建議/天賦/武器/組隊搭配 ▹璐洛洛◃,2021-12-21T13:30:15Z,https://www.youtube.com/watch?v=NOHX-uAJ2Xg,https://i.ytimg.com/vi/NOHX-uAJ2Xg/default.jpg
    // 杏仁ミル,35115,跟壞朋友們迎接2022年!!!!!!!,2021 - 12 - 31T18: 58:28Z,https://www.youtube.com/watch?v=gMyV3wvn4bg,https://i.ytimg.com/vi/gMyV3wvn4bg/default.jpg

    // create monthly directory first
    string fileDir = $"{savePath}/{currentDateTime:yyyy-MM}";
    Directory.CreateDirectory(fileDir);

    using StreamWriter writer = new($"{fileDir}/top-videos_{currentDateTime:yyyy-MM-dd-HH-mm-ss}.csv");
    using CsvWriter csv = new(writer, CultureInfo.InvariantCulture);
    csv.Context.RegisterClassMap<VideoInformationMap>();

    csv.WriteHeader<VideoInformation>();

    foreach (VideoInformation videoInfo in topVideoList.GetSortedList().OrderByDescending(e => e.ViewCount))
    {
        csv.NextRecord();
        csv.WriteRecord(videoInfo);
    }
}
