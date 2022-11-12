using Common.Types;
using Common.Utils;
using CsvHelper;
using System.Globalization;
using log4net;

ILog log = LogManager.GetLogger("");

try {
  string YouTubeApiKeyPath = args.Length >= 1 ? args[0] : "./DATA/YOUTUBE_API_KEY";
  string TwitchClientIdPath = args.Length >= 2 ? args[1] : "./DATA/TWITCH_CLIENT_ID";
  string TwitchSecretPath = args.Length >= 3 ? args[2] : "./DATA/TWITCH_SECRET";
  string trackListPath = args.Length >= 4 ? args[3] : "./DATA/TW_VTUBER_TRACK_LIST.csv";
  string excludeListPath = args.Length >= 5 ? args[4] : "./DATA/EXCLUDE_LIST.csv";
  string savePath = args.Length >= 6 ? args[5] : ".";

  log.Info("Configuration:");
  log.Info($"YouTubeApiKeyPath: {YouTubeApiKeyPath}");
  log.Info($"TwitchClientIdPath: {TwitchClientIdPath}");
  log.Info($"TwitchSecretPath: {TwitchSecretPath}");
  log.Info($"trackListPath: {trackListPath}");
  log.Info($"excludeListPath: {excludeListPath}");
  log.Info($"savePath: {savePath}");

  DateTime currentTime = DateTime.UtcNow;

  FetchYouTubeStatistics.Fetcher youtubeDataFetcher = new(FileUtility.GetSingleLineFromFile(YouTubeApiKeyPath), currentTime);
  FetchTwitchStatistics.Fetcher twitchDataFetcher = new(
      FileUtility.GetSingleLineFromFile(TwitchClientIdPath),
      FileUtility.GetSingleLineFromFile(TwitchSecretPath),
      currentTime);

  List<string> excluedList = FileUtility.GetListFromCsv(excludeListPath);
  log.Info($"excluedList: {string.Join(",", excluedList)}");
  TrackList trackList = new(csvFilePath: trackListPath, lstExcludeId: excluedList, throwOnValidationFail: true);
  log.Info($"trackList.GetCount(): {trackList.GetCount()}");

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
  WriteResult(lstStatistics, currentDateTime, savePath);
  WriteTopVideosListResult(topVideoList, currentDateTime, savePath);
  WriteLiveVideosListResult(liveVideosList, currentDateTime, savePath);
  log.Info("End program");
} catch (Exception e) {
  log.Error("Unhandled exception");
  log.Error(e.Message, e);
}

void WriteResult(List<VTuberStatistics> vtuberStatisticsList, DateTime currentDateTime, string savePath) {
  // CSV Format:
  // Display Name,View Count,Title,Publish Time,URL,Thumbnail URL
  // 鳥羽樂奈,40000,1000000,https://www.youtube.com/watch?v=SLRESBPj2v8
  // 香草奈若,25000,1000000,https://www.youtube.com/watch?v=SLRESBPj2v8

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
  // CSV Format:
  // Display Name,View Count,Title,Publish Time,URL,Thumbnail URL
  // 璐洛洛,39127,【原神研究室】五郎全分析🐶▸提供珍貴岩元素暴傷，只為岩系隊伍輔助的忠犬！聖遺物/命座建議/天賦/武器/組隊搭配 ▹璐洛洛◃,2021-12-21T13:30:15Z,https://www.youtube.com/watch?v=NOHX-uAJ2Xg,https://i.ytimg.com/vi/NOHX-uAJ2Xg/default.jpg
  // 杏仁ミル,35115,跟壞朋友們迎接2022年!!!!!!!,2021 - 12 - 31T18: 58:28Z,https://www.youtube.com/watch?v=gMyV3wvn4bg,https://i.ytimg.com/vi/gMyV3wvn4bg/default.jpg

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
  // CSV Format:
  // VTuber ID, Video Type,Title,Publish Time, URL, Thumbnail URL
  // c51f84b3ec9c4501a364a6a4982fa284, live,【中文 / English】元素使握著手把甦醒｜搖桿爬分 Controller Mode｜🌪風絮 FengXu,2022 - 06 - 06T05:29:28Z,https://www.twitch.tv/風絮_,https://static-cdn.jtvnw.net/previews-ttv/live_user_fengxu_vt-{width}x{height}.jpg
  // c51f84b3ec9c4501a364a6a4982fa284,live,【APEX RANK】元素使握著手把甦醒｜搖桿爬分 Controller Mode｜🌪風絮 FengXu,2022-06-06T05:30:00Z,https://www.youtube.com/watch?v=-xdwl1twrpU,https://i.ytimg.com/vi/-xdwl1twrpU/mqdefault_live.jpg
  // 60716febdfe949248e8f67d85634bec5,upcoming,【Lolipop】Awake Now／雄之助 (Cover)【歌ってみた】,2022 - 06 - 06T11: 30:00Z,https://www.youtube.com/watch?v=ffPuLoHf404,https://i.ytimg.com/vi/ffPuLoHf404/mqdefault.jpg

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
