using Common.Types;
using Common.Types.Basic;
using Common.Utils;
using FetchTwitchRecord.Extensions;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using LanguageExt.Pipes;
using log4net;
using System.Net;
using TwitchLib.Api;

class Program {
    private static readonly ILog log = LogManager.GetLogger(typeof(Program));

    static void Main(string[] args) {
        try {
            MainProcess(args);
        } catch (Exception e) {
            log.Error("Unhandled exception");
            log.Error(e.Message, e);
        }
    }

    static void MainProcess(string[] args) {
        string YouTubeApiKeyPath = args.Length >= 1 ? args[0] : "./DATA/YOUTUBE_API_KEY";
        string TwitchClientIdPath = args.Length >= 2 ? args[1] : "./DATA/TWITCH_CLIENT_ID";
        string TwitchSecretPath = args.Length >= 3 ? args[2] : "./DATA/TWITCH_SECRET";
        string trackListPath = args.Length >= 4 ? args[3] : "./DATA/TW_VTUBER_TRACK_LIST.csv";
        string excludeListPath = args.Length >= 5 ? args[4] : "./DATA/EXCLUDE_LIST.csv";
        string saveDir = args.Length >= 6 ? args[5] : "./DATA";

        log.Info("Configuration:");
        log.Info(YouTubeApiKeyPath);
        log.Info(TwitchClientIdPath);
        log.Info(TwitchSecretPath);
        log.Info(trackListPath);
        log.Info(excludeListPath);
        log.Info(saveDir);

        List<VTuberId> excluedList = FileUtility.GetListFromCsv(excludeListPath);
        TrackList trackList = new(csvFilePath: trackListPath, lstExcludeId: excluedList, throwOnValidationFail: true);

        log.Info($"Total entries: {trackList.GetCount()}");

        Dictionary<VTuberId, YouTubeData> dictYouTube = GenerateYouTubeDataDict(trackList, FileUtility.GetSingleLineFromFile(YouTubeApiKeyPath));

        TwitchAPI api = CreateTwitchApiInstance(
            clientId: FileUtility.GetSingleLineFromFile(TwitchClientIdPath),
            clientSecret: FileUtility.GetSingleLineFromFile(TwitchSecretPath));

        Dictionary<VTuberId, TwitchData> dictTwitch = GenerateTwitchDataDict(trackList, api);

        WriteBasicData(dictYouTube, dictTwitch, saveDir);
    }

    static void WriteBasicData(Dictionary<VTuberId, YouTubeData> dictYouTube, Dictionary<VTuberId, TwitchData> dictTwitch, string outputFileDir) {
        DateTime currentDateTime = DateTime.Now;

        // create monthly directory first
        string fileDir = $"{outputFileDir}/{currentDateTime:yyyy-MM}";
        Directory.CreateDirectory(fileDir);

        using StreamWriter writer = new($"{fileDir}/basic-data_{currentDateTime:yyyy-MM-dd-HH-mm-ss}.csv");

        VTuberBasicData.WriteToCsv(
            writer,
            MergeDictionary(dictYouTube, dictTwitch).Select(p => p.Value).ToList()
            );
    }

    // Key: VTuber ID
    static Dictionary<VTuberId, VTuberBasicData> MergeDictionary(Dictionary<VTuberId, YouTubeData> mainDict, Dictionary<VTuberId, TwitchData> minorDict) {
        Dictionary<VTuberId, VTuberBasicData> rDict = [];

        foreach (KeyValuePair<VTuberId, YouTubeData> pair in mainDict) {
            VTuberId VTuberId = pair.Key;
            YouTubeData youTubeData = pair.Value;

            if (minorDict.TryGetValue(VTuberId, out TwitchData value)) {
                rDict.Add(VTuberId, new VTuberBasicData(Id: VTuberId, YouTube: youTubeData, Twitch: value));
            } else {
                rDict.Add(VTuberId, new VTuberBasicData(Id: VTuberId, YouTube: youTubeData, Twitch: null));
            }
        }

        foreach (KeyValuePair<VTuberId, TwitchData> pair in minorDict) {
            VTuberId VTuberId = pair.Key;
            TwitchData twitchData = pair.Value;

            if (!rDict.ContainsKey(VTuberId)) {
                rDict.Add(VTuberId, new VTuberBasicData(Id: VTuberId, YouTube: null, Twitch: twitchData));
            }
        }

        return rDict;
    }

    static Dictionary<VTuberId, YouTubeData> GenerateYouTubeDataDict(TrackList trackList, string apiKey) {
        Dictionary<string, VTuberId> dictChannelIdVtuberId = GenerateYouTubeIdVTtuberIdDict(trackList);
        List<string> lstIdStringList = Generate50IdsStringList(dictChannelIdVtuberId.Keys.ToList());
        // initialize capacity
        Dictionary<VTuberId, YouTubeData> dictVTuberIdThumbnailUrl = new(dictChannelIdVtuberId.Count);

        YouTubeService youtubeService = new(new BaseClientService.Initializer() { ApiKey = apiKey });
        foreach (string idStringList in lstIdStringList) {
            ChannelsResource.ListRequest channelListRequest = youtubeService.Channels.List("snippet, statistics");
            channelListRequest.Id = idStringList;
            channelListRequest.MaxResults = 50;

            Google.Apis.YouTube.v3.Data.ChannelListResponse? channellistItemsListResponse = ExecuteYouTubeThrowableWithRetry(() => channelListRequest.Execute());
            // channellistItemsListResponse.Items is actually nullable
            if (channellistItemsListResponse?.Items is null) {
                continue;
            }

            foreach (Google.Apis.YouTube.v3.Data.Channel channelItem in channellistItemsListResponse.Items) {
                string channelId = channelItem.Id;
                ulong? subscriberCount = channelItem.Statistics.SubscriberCount;
                ulong? viewCount = channelItem.Statistics.ViewCount;
                string thumbnailUrl = channelItem.Snippet.Thumbnails.Default__.Url;

                bool hasId = dictChannelIdVtuberId.TryGetValue(channelId, out VTuberId? VTuberId);
                if (hasId && VTuberId is not null && !dictVTuberIdThumbnailUrl.ContainsKey(VTuberId)) {
                    dictVTuberIdThumbnailUrl.Add(VTuberId, new YouTubeData(SubscriberCount: subscriberCount, ViewCount: viewCount, ThumbnailUrl: thumbnailUrl));
                }
            }
        }

        return dictVTuberIdThumbnailUrl;
    }

    static Dictionary<VTuberId, TwitchData> GenerateTwitchDataDict(TrackList trackList, TwitchAPI api) {
        Dictionary<string, VTuberId> dictChannelIdVtuberId = GenerateTwitchIdVTuberIdDict(trackList);
        List<List<string>> lstIdStringList = Generate100IdsStringListList(dictChannelIdVtuberId.Keys.ToList());
        // initialize capacity
        Dictionary<VTuberId, TwitchData> dictNameThumbnailUrl = new(dictChannelIdVtuberId.Count);

        foreach (List<string> idStringList in lstIdStringList) {
            Dictionary<string, TwitchData> dictTwitch = GetTwitchIdThumbnailUrlDict(idStringList, api);

            foreach (KeyValuePair<string, TwitchData> pair in dictTwitch) {
                string channelId = pair.Key;
                TwitchData twitchData = pair.Value;

                bool hasId = dictChannelIdVtuberId.TryGetValue(channelId, out VTuberId? VTuberId);
                if (hasId && VTuberId is not null && !dictNameThumbnailUrl.ContainsKey(VTuberId)) {
                    dictNameThumbnailUrl.Add(VTuberId, twitchData);
                }
            }
        }

        return dictNameThumbnailUrl;
    }

    // Key: Twitch channel ID
    static Dictionary<string, TwitchData> GetTwitchIdThumbnailUrlDict(List<string> userIdList, TwitchAPI api) {
        TwitchLib.Api.Helix.Models.Users.GetUsers.GetUsersResponse? getUsersResponse = api.GetUsers(userIdList, log);

        if (getUsersResponse == null) {
            return [];
        }

        Dictionary<string, TwitchData> rDict = new(getUsersResponse.Users.Length);
        log.Info($"Response user count is {getUsersResponse.Users.Length}");
        foreach (TwitchLib.Api.Helix.Models.Users.GetUsers.User user in getUsersResponse.Users) {
            ulong? nullableFollowerCount = api.GetChannelFollwerCount(
                broadcasterId: user.Id,
                log: log
                );

            log.Info($"Follower Count of {user.Id} is {nullableFollowerCount}");
            rDict.Add(user.Id, new TwitchData(FollowerCount: nullableFollowerCount ?? 0ul, ThumbnailUrl: user.ProfileImageUrl));
        }

        return rDict;
    }

    // Key: YouTube Channel ID, Value: VTuber ID
    static Dictionary<string, VTuberId> GenerateYouTubeIdVTtuberIdDict(TrackList trackList) {
        List<VTuberId> lstId = trackList.GetIdList();

        // initialize capacity
        Dictionary<string, VTuberId> rDict = new(lstId.Count);

        foreach (VTuberId id in lstId) {
            string channelId = trackList.GetYouTubeChannelId(id);
            if (channelId == "") {
                continue;
            }

            rDict.Add(channelId, id);
        }

        return rDict;
    }

    // Key: Twitch Channel ID, Value: VTuber ID
    static Dictionary<string, VTuberId> GenerateTwitchIdVTuberIdDict(TrackList trackList) {
        List<VTuberId> lstId = trackList.GetIdList();

        // initialize capacity
        Dictionary<string, VTuberId> rDict = new(lstId.Count);

        foreach (VTuberId id in lstId) {
            string channelId = trackList.GetTwitchChannelId(id);
            if (channelId == "") {
                continue;
            }

            rDict.Add(channelId, id);
        }

        return rDict;
    }

    static List<string> Generate50IdsStringList(List<string> keyList) {
        return keyList.Chunk(50).Map(e => string.Join(',', e)).ToList();
    }

    static List<List<string>> Generate100IdsStringListList(List<string> keyList) {
        return keyList.Chunk(100).Map(e => e.ToList()).ToList();
    }

    static T? ExecuteYouTubeThrowableWithRetry<T>(Func<T> func) where T : class? {
        int RETRY_TIME = 10;
        TimeSpan RETRY_DELAY = new(hours: 0, minutes: 0, seconds: 3);

        for (int i = 0; i < RETRY_TIME; i++) {
            try {
                return func.Invoke();
            } catch (Google.GoogleApiException e) {
                if (e.HttpStatusCode == HttpStatusCode.NotFound) {
                    log.Warn($"Request HttpStatusCode is HttpStatusCode.NotFound.");
                    return null;
                }
            } catch (Exception e) {
                log.Warn($"Failed to execute {func.Method.Name}. {i} tries. Retry after {RETRY_DELAY.TotalSeconds} seconds");
                log.Warn(e.Message, e);
                Task.Delay(RETRY_DELAY);
            }
        }

        return null;
    }

    private static TwitchAPI CreateTwitchApiInstance(string clientId, string clientSecret) {
        TwitchAPI api = new();
        api.Settings.ClientId = clientId;
        api.Settings.Secret = clientSecret;

        return api;
    }
}