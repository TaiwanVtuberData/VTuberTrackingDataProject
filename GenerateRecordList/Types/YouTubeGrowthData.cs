namespace GenerateRecordList.Types;

public record YouTubeGrowthData(
    string id,
    BaseCountType subscriber,
    GrowthData _7DaysGrowth,
    GrowthData _30DaysGrowth,
    string? Nationality)
    : YouTubeData(
        id: id,
        subscriber: subscriber);
