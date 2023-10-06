namespace GenerateRecordList.Types;

public record YouTubeSubscriberCountToPopularityData(
    string id,
    BaseCountType subscriber,
    decimal popularity)
    : YouTubeData(
        id: id,
        subscriber: subscriber);
