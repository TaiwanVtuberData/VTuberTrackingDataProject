namespace GenerateRecordList.Types;

public record TwitchData(
    string id,
    BaseCountType follower)
    : BaseTwitchData(id);
