using GenerateRecordList.Types;

namespace GenerateYearEndReport.Types;

public record YearEndYouTubeViewCountGrowthData(
    string id,
    ulong totalViewCount,
    GrowthData _1YearGrowth,
    string? Nationality
);
