using GenerateRecordList.Types;

namespace GenerateYearEndReport.Types;

public record YearEndYouTubeGrowthData(
    string id,
    BaseCountType subscriber,
    GrowthData _1YearGrowth,
    string? Nationality
) : YouTubeData(id: id, subscriber: subscriber);
