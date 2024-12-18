using GenerateRecordList.Types;

namespace GenerateYearEndReport.Types;

public record YearEndYouTubeViewCountGrowthData(
    string id,
    ulong totalViewCount,
    GrowthData _365DaysGrowth,
    string? Nationality
);
