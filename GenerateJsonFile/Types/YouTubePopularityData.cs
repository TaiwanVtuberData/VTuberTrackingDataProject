namespace GenerateJsonFile.Types;

internal record YouTubePopularityData(
    string id,
    BaseCountType subscriber,
    ulong popularity,
    ulong liveStreamPopularity,
    ulong videoPopularity)
    : YouTubeData(
        id: id,
        subscriber: subscriber);
