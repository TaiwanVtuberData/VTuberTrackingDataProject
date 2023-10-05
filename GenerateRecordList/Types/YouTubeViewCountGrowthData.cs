namespace GenerateRecordList.Types;

public record YouTubeViewCountGrowthData(
    string id,
    ulong totalViewCount,
    GrowthData _7DaysGrowth,
    GrowthData _30DaysGrowth,
    string? Nationality);
