namespace GenerateJsonFile.Types;

internal record TwitchData(
    string id,
    BaseCountType follower)
    : BaseTwitchData(id);
