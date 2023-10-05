namespace GenerateRecordList.Types;

public record YouTubePopularityData(
    string id,
    BaseCountType subscriber,
    ulong popularity)
    : YouTubeData(
        id: id,
        subscriber: subscriber);
