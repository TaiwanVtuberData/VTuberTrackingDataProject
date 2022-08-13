using Common.Types;
using Common.Utils;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using TwitchLib.Api;

string YouTubeApiKeyPath = args.Length >= 1 ? args[0] : "./DATA/YOUTUBE_API_KEY";
string TwitchClientIdPath = args.Length >= 2 ? args[1] : "./DATA/TWITCH_CLIENT_ID";
string TwitchSecretPath = args.Length >= 3 ? args[2] : "./DATA/TWITCH_SECRET";
string trackListPath = args.Length >= 4 ? args[3] : "./DATA/TW_VTUBER_TRACK_LIST.csv";
string excludeListPath = args.Length >= 5 ? args[4] : "./DATA/EXCLUDE_LIST.csv";
string saveDir = args.Length >= 6 ? args[5] : "./DATA";

Console.WriteLine("Configuration:");
Console.WriteLine(YouTubeApiKeyPath);
Console.WriteLine(TwitchClientIdPath);
Console.WriteLine(TwitchSecretPath);
Console.WriteLine(trackListPath);
Console.WriteLine(excludeListPath);
Console.WriteLine(saveDir);

List<string> excluedList = FileUtility.GetListFromCsv(excludeListPath);
TrackList trackList = new(csvFilePath: trackListPath, lstExcludeId: excluedList, throwOnValidationFail: true);

Console.WriteLine($"Total entries: {trackList.GetCount()}");

Dictionary<string, YouTubeData> dictYouTube = GenerateYouTubeDataDict(trackList, FileUtility.GetSingleLineFromFile(YouTubeApiKeyPath));
Dictionary<string, TwitchData> dictTwitch = GenerateTwitchDataDict(trackList,
    FileUtility.GetSingleLineFromFile(TwitchClientIdPath),
    FileUtility.GetSingleLineFromFile(TwitchSecretPath));

WriteBasicData(trackList, dictYouTube, dictTwitch, saveDir);

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

    Google.Apis.YouTube.v3.Data.ChannelListResponse channellistItemsListResponse = channelListRequest.Execute();
    // channellistItemsListResponse.Items is actually nullable
    if (channellistItemsListResponse.Items is null) {
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

  TwitchAPI api = new();
  api.Settings.ClientId = clientId;
  api.Settings.Secret = secret;

  foreach (List<string> idStringList in lstIdStringList) {
    Dictionary<string, TwitchData> dictTwitch = GetTwitchIdThumbnailUrlDict(idStringList, api);

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
static Dictionary<string, TwitchData> GetTwitchIdThumbnailUrlDict(List<string> lstUserId, TwitchAPI api) {
  TwitchLib.Api.Helix.Models.Users.GetUsers.GetUsersResponse? userResponseResult = null;

  bool hasResponse = false;
  for (int i = 0; i < 2; i++) {
    try {
      var userResponse =
          api.Helix.Users.GetUsersAsync(lstUserId);
      userResponseResult = userResponse.Result;

      hasResponse = true;
      break;
    } catch {
    }
  }

  if (!hasResponse || userResponseResult is null) {
    return new();
  }

  Dictionary<string, TwitchData> rDict = new(userResponseResult.Users.Length);
  foreach (TwitchLib.Api.Helix.Models.Users.GetUsers.User user in userResponseResult.Users) {
    if (!rDict.ContainsKey(user.Id)) {
      TwitchLib.Api.Helix.Models.Users.GetUserFollows.GetUsersFollowsResponse? usersFollowsResponseResult = null;

      hasResponse = false;
      for (int i = 0; i < 2; i++) {
        try {
          var usersFollowsResponse =
              api.Helix.Users.GetUsersFollowsAsync(
                  first: 100,
                  toId: user.Id
                  );
          usersFollowsResponseResult = usersFollowsResponse.Result;

          hasResponse = true;
          break;
        } catch {
        }
      }

      if (!hasResponse || usersFollowsResponseResult is null) {
        return new();
      }

      rDict.Add(user.Id, new TwitchData(FollowerCount: (ulong)usersFollowsResponseResult.TotalFollows, ThumbnailUrl: user.ProfileImageUrl));
    }
  }

  return rDict;
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