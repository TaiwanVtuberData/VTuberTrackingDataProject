using Common.Types;
using Common.Utils;
using FetchBasicData.Types;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using LanguageExt.Pipes;
using log4net;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
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

        List<string> excluedList = FileUtility.GetListFromCsv(excludeListPath);
        TrackList trackList = new(csvFilePath: trackListPath, lstExcludeId: excluedList, throwOnValidationFail: true);

        log.Info($"Total entries: {trackList.GetCount()}");

        Dictionary<string, YouTubeData> dictYouTube = GenerateYouTubeDataDict(trackList, FileUtility.GetSingleLineFromFile(YouTubeApiKeyPath));
        Dictionary<string, TwitchData> dictTwitch = GenerateTwitchDataDict(trackList,
            FileUtility.GetSingleLineFromFile(TwitchClientIdPath),
            FileUtility.GetSingleLineFromFile(TwitchSecretPath));

        WriteBasicData(trackList, dictYouTube, dictTwitch, saveDir);
    }

    static void WriteBasicData(TrackList trackList, Dictionary<string, YouTubeData> dictYouTube, Dictionary<string, TwitchData> dictTwitch, string outputFileDir) {
        DateTime currentDateTime = DateTime.Now;

        // create monthly directory first
        string fileDir = $"{outputFileDir}/{currentDateTime:yyyy-MM}";
        Directory.CreateDirectory(fileDir);

        using StreamWriter writer = new($"{fileDir}/basic-data_{currentDateTime:yyyy-MM-dd-HH-mm-ss}.csv");

        VTuberBasicData.WriteToCsv(
            writer,
            MergeDictionary(trackList, dictYouTube, dictTwitch).Select(p => p.Value).ToList()
            );
    }

    // Key: VTuber ID
    static Dictionary<string, VTuberBasicData> MergeDictionary(TrackList trackList, Dictionary<string, YouTubeData> mainDict, Dictionary<string, TwitchData> minorDict) {
        Dictionary<string, VTuberBasicData> rDict = new();

        foreach (KeyValuePair<string, YouTubeData> pair in mainDict) {
            string VTuberId = pair.Key;
            YouTubeData youTubeData = pair.Value;

            if (minorDict.ContainsKey(VTuberId)) {
                rDict.Add(VTuberId, new VTuberBasicData(Id: VTuberId, YouTube: youTubeData, Twitch: minorDict[VTuberId]));
            } else {
                rDict.Add(VTuberId, new VTuberBasicData(Id: VTuberId, YouTube: youTubeData, Twitch: null));
            }
        }

        foreach (KeyValuePair<string, TwitchData> pair in minorDict) {
            string VTuberId = pair.Key;
            TwitchData twitchData = pair.Value;

            if (!rDict.ContainsKey(VTuberId)) {
                rDict.Add(VTuberId, new VTuberBasicData(Id: VTuberId, YouTube: null, Twitch: twitchData));
            }
        }

        return rDict;
    }

    // Key: VTuber ID
    static Dictionary<string, YouTubeData> GenerateYouTubeDataDict(TrackList trackList, string apiKey) {
        Dictionary<string, string> dictChannelIdVtuberId = GenerateYouTubeIdVTtuberIdDict(trackList);
        List<string> lstIdStringList = Generate50IdsStringList(dictChannelIdVtuberId.Keys.ToList());
        // initialize capacity
        Dictionary<string, YouTubeData> dictVTuberIdThumbnailUrl = new(dictChannelIdVtuberId.Count);

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

                bool hasId = dictChannelIdVtuberId.TryGetValue(channelId, out string? VTuberId);
                if (hasId && VTuberId is not null && !dictVTuberIdThumbnailUrl.ContainsKey(VTuberId)) {
                    dictVTuberIdThumbnailUrl.Add(VTuberId, new YouTubeData(SubscriberCount: subscriberCount, ViewCount: viewCount, ThumbnailUrl: thumbnailUrl));
                }
            }
        }

        return dictVTuberIdThumbnailUrl;
    }

    // Key: VTuber ID
    static Dictionary<string, TwitchData> GenerateTwitchDataDict(TrackList trackList, string clientId, string secret) {
        Dictionary<string, string> dictChannelIdVtuberId = GenerateTwitchIdVTuberIdDict(trackList);
        List<List<string>> lstIdStringList = Generate100IdsStringListList(dictChannelIdVtuberId.Keys.ToList());
        // initialize capacity
        Dictionary<string, TwitchData> dictNameThumbnailUrl = new(dictChannelIdVtuberId.Count);

        foreach (List<string> idStringList in lstIdStringList) {
            Dictionary<string, TwitchData> dictTwitch = GetTwitchIdThumbnailUrlDict(idStringList, clientId, secret);

            foreach (KeyValuePair<string, TwitchData> pair in dictTwitch) {
                string channelId = pair.Key;
                TwitchData twitchData = pair.Value;

                bool hasId = dictChannelIdVtuberId.TryGetValue(channelId, out string? VTuberId);
                if (hasId && VTuberId is not null && !dictNameThumbnailUrl.ContainsKey(VTuberId)) {
                    dictNameThumbnailUrl.Add(VTuberId, twitchData);
                }
            }
        }

        return dictNameThumbnailUrl;
    }

    // Key: Twitch channel ID
    static Dictionary<string, TwitchData> GetTwitchIdThumbnailUrlDict(List<string> lstUserId, string clientId, string secret) {
        TwitchLib.Api.Helix.Models.Users.GetUsers.GetUsersResponse? userResponseResult = null;

        bool hasResponse = false;
        for (int i = 0; i < 2; i++) {
            try {
                TwitchAPI api = CreateTwitchApiInstance(clientId, secret);

                var userResponse =
                    api.Helix.Users.GetUsersAsync(lstUserId);
                userResponseResult = userResponse.Result;

                hasResponse = true;
                break;
            } catch (Exception e) {
                log.Error(e.Message, e);
            }
        }

        if (!hasResponse || userResponseResult is null) {
            return new();
        }

        Dictionary<string, TwitchData> rDict = new(userResponseResult.Users.Length);
        log.Info($"Response user count is {userResponseResult.Users.Length}");
        foreach (TwitchLib.Api.Helix.Models.Users.GetUsers.User user in userResponseResult.Users) {
            if (!rDict.ContainsKey(user.Id)) {
                string? accessToken = GetTwitchAccessToken(clientId, secret);

                if (accessToken != null) {
                    ulong? followerCount = GetTwitchFollowerCount(user.Id, clientId, accessToken);
                    log.Info($"Follower Count of {user.Id} is {followerCount}");
                    rDict.Add(user.Id, new TwitchData(FollowerCount: followerCount ?? 0ul, ThumbnailUrl: user.ProfileImageUrl));
                }
            }
        }

        return rDict;
    }

    static string? GetTwitchAccessToken(string clientId, string clientSecret) {
        HttpRequestMessage request = new(HttpMethod.Post, "https://id.twitch.tv/oauth2/token") {
            Content = new FormUrlEncodedContent(
            new Dictionary<string, string> {
            { "client_id", clientId },
            { "client_secret", clientSecret },
            { "grant_type", "client_credentials" },
            }
            )
        };

        return ExecuteTwitchThrowableWithRetry(() => {
            HttpResponseMessage response = new HttpClient()
                .SendAsync(request)
                .Result
                .EnsureSuccessStatusCode();


            return JsonSerializer
            .Deserialize<TwitchOauth2Response>(response.Content.ReadAsStringAsync().Result)
            ?.access_token;
        });
    }

    static ulong? GetTwitchFollowerCount(string broadcasterId, string clientId, string accessToken) {
        // don't know why query parameter doesn't work like the method in GetTwitchAccessToken
        HttpRequestMessage request = new(HttpMethod.Get, $"https://api.twitch.tv/helix/channels/followers?broadcaster_id={broadcasterId}");

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("Client-Id", clientId);

        // don't know why ulong does not fit generic constraint either
        TwitchFollowerCountResponse? response = ExecuteTwitchThrowableWithRetry(() => {
            HttpResponseMessage response = new HttpClient()
                .SendAsync(request)
                .Result;

            response.EnsureSuccessStatusCode();

            return JsonSerializer
            .Deserialize<TwitchFollowerCountResponse>(response.Content.ReadAsStringAsync().Result);
        });

        return response?.total;
    }

    // Key: YouTube Channel ID, Value: VTuber ID
    static Dictionary<string, string> GenerateYouTubeIdVTtuberIdDict(TrackList trackList) {
        List<string> lstId = trackList.GetIdList();

        // initialize capacity
        Dictionary<string, string> rDict = new(lstId.Count);

        foreach (string id in lstId) {
            string channelId = trackList.GetYouTubeChannelId(id);
            if (channelId == "") {
                continue;
            }

            rDict.Add(channelId, id);
        }

        return rDict;
    }

    // Key: Twitch Channel ID, Value: VTuber ID
    static Dictionary<string, string> GenerateTwitchIdVTuberIdDict(TrackList trackList) {
        List<string> lstId = trackList.GetIdList();

        // initialize capacity
        Dictionary<string, string> rDict = new(lstId.Count);

        foreach (string id in lstId) {
            string channelId = trackList.GetTwitchChannelId(id);
            if (channelId == "") {
                continue;
            }

            rDict.Add(channelId, id);
        }

        return rDict;
    }

    static List<string> Generate50IdsStringList(List<string> KeyList) {
        List<string> ans = new();

        int index;
        // pack 50 ids into a string
        for (index = 0; index < (KeyList.Count) / 50 * 50; index += 50) {
            string idRequestString = "";
            for (int offset = 0; offset < 50; offset++)
                idRequestString += KeyList[index + offset] + ',';
            idRequestString = idRequestString.Substring(0, idRequestString.Length - 1);
            ans.Add(idRequestString);
        }

        // residual
        if (KeyList.Count % 50 != 0) {
            string idRequestStringRes = "";
            for (; index < KeyList.Count; index++) {
                idRequestStringRes += KeyList[index] + ',';
            }
            idRequestStringRes = idRequestStringRes.Substring(0, idRequestStringRes.Length - 1);
            ans.Add(idRequestStringRes);
        }

        return ans;
    }

    static List<List<string>> Generate100IdsStringListList(List<string> KeyList) {
        List<List<string>> ans = new();

        int index;
        // pack 100 ids into a List<string>
        for (index = 0; index < (KeyList.Count) / 100 * 100; index += 100) {
            List<string> lstId = new();
            for (int offset = 0; offset < 100; offset++) {
                lstId.Add(KeyList[index + offset]);
            }

            ans.Add(lstId);
        }

        // residual
        if (KeyList.Count % 100 != 0) {
            List<string> lstId = new();
            for (; index < KeyList.Count; index++) {
                lstId.Add(KeyList[index]);
            }

            ans.Add(lstId);
        }

        return ans;
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

    private static T? ExecuteTwitchThrowableWithRetry<T>(Func<T> func) where T : class? {
        int RETRY_TIME = 10;
        TimeSpan RETRY_DELAY = new(hours: 0, minutes: 0, seconds: 3);

        for (int i = 0; i < RETRY_TIME; i++) {
            try {
                return func.Invoke();
            } catch (HttpRequestException e) {
                log.Warn($"Request HttpStatusCode is {e.StatusCode}.");
                log.Warn($"Failed to execute {func.Method.Name}. {i} tries. Retry after {RETRY_DELAY.TotalSeconds} seconds.");
                log.Warn(e.Message, e);
                Task.Delay(RETRY_DELAY);
            } catch (Exception e) {
                log.Warn($"Failed to execute {func.Method.Name}. {i} tries. Retry after {RETRY_DELAY.TotalSeconds} seconds.");
                log.Warn(e.Message, e);
                Task.Delay(RETRY_DELAY);
            }
        }

        return null;
    }
}