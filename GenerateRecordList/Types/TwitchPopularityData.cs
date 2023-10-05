namespace GenerateRecordList.Types;

public record TwitchPopularityData(
    string id,
    BaseCountType follower,
    ulong popularity)
    : TwitchData(
        id: id,
        follower: follower);
