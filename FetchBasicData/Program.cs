using Common.Types;
using Common.Utils;
using FetchBasicData;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using TwitchLib.Api;

string trackListPath = args.Length >= 1 ? args[0] : "./DATA/TW_VTUBER_TRACK_LIST.csv";
string YouTubeApiKeyPath = args.Length >= 2 ? args[1] : "./DATA/API_KEY";
string TwitchClientIdPath = args.Length >= 3 ? args[2] : "./DATA/TWITCH_CLIENT_ID";
string TwitchSecretPath = args.Length >= 4 ? args[3] : "./DATA/TWITCH_SECRET";
string newSavePath = args.Length >= 5 ? args[4] : "./DATA";

Console.WriteLine(trackListPath);
Console.WriteLine(YouTubeApiKeyPath);
Console.WriteLine(TwitchClientIdPath);
Console.WriteLine(TwitchSecretPath);
Console.WriteLine(newSavePath);

TrackList trackList = new(trackListPath, requiredLevel: 999, throwOnValidationFail: true);

Console.WriteLine($"Total entries: {trackList.GetCount()}");

Dictionary<string, YouTubeData> dictYouTubeNameData = GenerateYouTubeNameDataDict(trackList, FileUtility.GetSingleLineFromFile(YouTubeApiKeyPath));
Dictionary<string, TwitchData> dictTwitchNameData = GenerateTwitchNameDataDict(trackList,
    FileUtility.GetSingleLineFromFile(TwitchClientIdPath),
    FileUtility.GetSingleLineFromFile(TwitchSecretPath));

WriteNewFormat(newSavePath, dictYouTubeNameData, dictTwitchNameData);

static void WriteNewFormat(string outputFileDir, Dictionary<string, YouTubeData> dictYouTube, Dictionary<string, TwitchData> dictTwitch)
{
    DateTime currentDateTime = DateTime.Now;

    // create monthly directory first
    string fileDir = $"{outputFileDir}/{currentDateTime:yyyy-MM}";
    Directory.CreateDirectory(fileDir);

    using StreamWriter writer = new($"{fileDir}/basic-data_{currentDateTime:yyyy-MM-dd-HH-mm-ss}.csv");

    writer.WriteLine(
        "Display Name," +
        "YouTube Subscriber Count," +
        "YouTube View Count," +
        "YouTube Thumbnail URL," +
        "Twitch Follower Count," +
        "Twitch Thumbnail URL");

    foreach (KeyValuePair<string, Data> pair in MergeDictionaryNew(dictYouTube, dictTwitch).OrderBy(p => p.Key))
    {
        string displayName = pair.Key;
        Data data = pair.Value;

        writer.WriteLine($"{displayName}," +
            $"{(data.YouTube.HasValue ? data.YouTube.Value.SubscriberCount : "")}," +
            $"{(data.YouTube.HasValue ? data.YouTube.Value.ViewCount : "")}," +
            $"{(data.YouTube.HasValue ? data.YouTube.Value.ThumbnailUrl : "")}," +
            $"{(data.Twitch.HasValue ? data.Twitch.Value.FollowerCount : "")}," +
            $"{(data.Twitch.HasValue ? data.Twitch.Value.ThumbnailUrl : "")}");
    }
    writer.Close();
}


static Dictionary<string, Data> MergeDictionaryNew(Dictionary<string, YouTubeData> mainDict, Dictionary<string, TwitchData> minorDict)
{
    Dictionary<string, Data> rDict = new();

    foreach (KeyValuePair<string, YouTubeData> pair in mainDict)
    {
        string displayName = pair.Key;
        YouTubeData youTubeData = pair.Value;

        if (minorDict.ContainsKey(displayName))
        {
            rDict.Add(displayName, new Data(YouTube: youTubeData, Twitch: minorDict[displayName]));
        }
        else
        {
            rDict.Add(displayName, new Data(YouTube: youTubeData, Twitch: null));
        }
    }

    foreach (KeyValuePair<string, TwitchData> pair in minorDict)
    {
        string displayName = pair.Key;
        TwitchData twitchData = pair.Value;

        if (!rDict.ContainsKey(displayName))
        {
            rDict.Add(displayName, new Data(YouTube: null, Twitch: twitchData));
        }
    }

    return rDict;
}

static Dictionary<string, YouTubeData> GenerateYouTubeNameDataDict(TrackList trackList, string apiKey)
{
    Dictionary<string, string> dictChannelIdName = GenerateYouTubeIdNameDict(trackList);
    List<string> lstIdStringList = Generate50IdsStringList(dictChannelIdName.Keys.ToList());
    // initialize capacity
    Dictionary<string, YouTubeData> dictNameThumbnailUrl = new(dictChannelIdName.Count);

    YouTubeService youtubeService = new(new BaseClientService.Initializer() { ApiKey = apiKey });
    foreach (string idStringList in lstIdStringList)
    {
        ChannelsResource.ListRequest channelListRequest = youtubeService.Channels.List("snippet, statistics");
        channelListRequest.Id = idStringList;
        channelListRequest.MaxResults = 50;

        Google.Apis.YouTube.v3.Data.ChannelListResponse channellistItemsListResponse = channelListRequest.Execute();
        // channellistItemsListResponse.Items is actually nullable
        if (channellistItemsListResponse.Items is null)
        {
            continue;
        }

        foreach (Google.Apis.YouTube.v3.Data.Channel channelItem in channellistItemsListResponse.Items)
        {
            string channelId = channelItem.Id;
            ulong? subscriberCount = channelItem.Statistics.SubscriberCount;
            ulong? viewCount = channelItem.Statistics.ViewCount;
            string thumbnailUrl = channelItem.Snippet.Thumbnails.Default__.Url;

            bool hasId = dictChannelIdName.TryGetValue(channelId, out string? displayName);
            if (hasId && displayName is not null && !dictNameThumbnailUrl.ContainsKey(displayName))
            {
                dictNameThumbnailUrl.Add(displayName, new YouTubeData(SubscriberCount: subscriberCount, ViewCount: viewCount, ThumbnailUrl: thumbnailUrl));
            }
        }
    }

    return dictNameThumbnailUrl;
}

static Dictionary<string, TwitchData> GenerateTwitchNameDataDict(TrackList trackList, string clientId, string secret)
{
    Dictionary<string, string> dictChannelIdName = GenerateTwitchIdNameDict(trackList);
    List<List<string>> lstIdStringList = Generate100IdsStringListList(dictChannelIdName.Keys.ToList());
    // initialize capacity
    Dictionary<string, TwitchData> dictNameThumbnailUrl = new(dictChannelIdName.Count);

    TwitchAPI api = new();
    api.Settings.ClientId = clientId;
    api.Settings.Secret = secret;

    foreach (List<string> idStringList in lstIdStringList)
    {
        Dictionary<string, TwitchData> dictTwitch = GetTwitchIdThumbnailUrlDict(idStringList, api);

        foreach (KeyValuePair<string, TwitchData> pair in dictTwitch)
        {
            string channelId = pair.Key;
            TwitchData twitchData = pair.Value;

            bool hasId = dictChannelIdName.TryGetValue(channelId, out string? displayName);
            if (hasId && displayName is not null && !dictNameThumbnailUrl.ContainsKey(displayName))
            {
                dictNameThumbnailUrl.Add(displayName, twitchData);
            }
        }
    }

    return dictNameThumbnailUrl;
}

static Dictionary<string, TwitchData> GetTwitchIdThumbnailUrlDict(List<string> lstUserId, TwitchAPI api)
{
    TwitchLib.Api.Helix.Models.Users.GetUsers.GetUsersResponse? userResponseResult = null;

    bool hasResponse = false;
    for (int i = 0; i < 2; i++)
    {
        try
        {
            var userResponse =
                api.Helix.Users.GetUsersAsync(lstUserId);
            userResponseResult = userResponse.Result;

            hasResponse = true;
            break;
        }
        catch
        {
        }
    }

    if (!hasResponse || userResponseResult is null)
    {
        return new();
    }

    Dictionary<string, TwitchData> rDict = new(userResponseResult.Users.Length);
    foreach (TwitchLib.Api.Helix.Models.Users.GetUsers.User user in userResponseResult.Users)
    {
        if (!rDict.ContainsKey(user.Id))
        {
            TwitchLib.Api.Helix.Models.Users.GetUserFollows.GetUsersFollowsResponse? usersFollowsResponseResult = null;

            hasResponse = false;
            for (int i = 0; i < 2; i++)
            {
                try
                {
                    var usersFollowsResponse =
                        api.Helix.Users.GetUsersFollowsAsync(
                            first: 100,
                            toId: user.Id
                            );
                    usersFollowsResponseResult = usersFollowsResponse.Result;

                    hasResponse = true;
                    break;
                }
                catch
                {
                }
            }

            if (!hasResponse || usersFollowsResponseResult is null)
            {
                return new();
            }

            rDict.Add(user.Id, new TwitchData(FollowerCount: (ulong)usersFollowsResponseResult.TotalFollows, ThumbnailUrl: user.ProfileImageUrl));
        }
    }

    return rDict;
}

static Dictionary<string, string> GenerateYouTubeIdNameDict(TrackList trackList)
{
    List<string> displayNameList = trackList.GetDisplayNameList();

    // initialize capacity
    Dictionary<string, string> rDict = new(displayNameList.Count);

    foreach (string displayName in displayNameList)
    {
        string channelId = trackList.GetYouTubeChannelIdByName(displayName);
        if (channelId == "")
        {
            continue;
        }

        rDict.Add(channelId, displayName);
    }

    return rDict;
}

static Dictionary<string, string> GenerateTwitchIdNameDict(TrackList trackList)
{
    List<string> displayNameList = trackList.GetDisplayNameList();

    // initialize capacity
    Dictionary<string, string> rDict = new(displayNameList.Count);

    foreach (string displayName in displayNameList)
    {
        string channelId = trackList.GetTwitchChannelIdByName(displayName);
        if (channelId == "")
        {
            continue;
        }

        rDict.Add(channelId, displayName);
    }

    return rDict;
}

static List<string> Generate50IdsStringList(List<string> KeyList)
{
    List<string> ans = new();

    int index;
    // pack 50 ids into a string
    for (index = 0; index < (KeyList.Count) / 50 * 50; index += 50)
    {
        string idRequestString = "";
        for (int offset = 0; offset < 50; offset++)
            idRequestString += KeyList[index + offset] + ',';
        idRequestString = idRequestString.Substring(0, idRequestString.Length - 1);
        ans.Add(idRequestString);
    }

    // residual
    if (KeyList.Count % 50 != 0)
    {
        string idRequestStringRes = "";
        for (; index < KeyList.Count; index++)
        {
            idRequestStringRes += KeyList[index] + ',';
        }
        idRequestStringRes = idRequestStringRes.Substring(0, idRequestStringRes.Length - 1);
        ans.Add(idRequestStringRes);
    }

    return ans;
}

static List<List<string>> Generate100IdsStringListList(List<string> KeyList)
{
    List<List<string>> ans = new();

    int index;
    // pack 100 ids into a List<string>
    for (index = 0; index < (KeyList.Count) / 100 * 100; index += 100)
    {
        List<string> lstId = new();
        for (int offset = 0; offset < 100; offset++)
        {
            lstId.Add(KeyList[index + offset]);
        }

        ans.Add(lstId);
    }

    // residual
    if (KeyList.Count % 100 != 0)
    {
        List<string> lstId = new();
        for (; index < KeyList.Count; index++)
        {
            lstId.Add(KeyList[index]);
        }

        ans.Add(lstId);
    }

    return ans;
}