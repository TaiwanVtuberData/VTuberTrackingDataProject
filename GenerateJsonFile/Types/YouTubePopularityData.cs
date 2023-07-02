namespace GenerateJsonFile.Types;

internal record YouTubePopularityData(
    string id,
    BaseCountType subscriber,
    ulong popularity,
    ulong livestreamPopularity,
    ulong videoPopularity)
    : YouTubeData(
        id: id,
        subscriber: subscriber);
