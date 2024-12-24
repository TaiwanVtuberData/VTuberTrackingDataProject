using GenerateRecordList.Types;

namespace GenerateYearEndReport.Types;

public record YearEndTwitchGrowthData(
    string id,
    BaseCountType follower,
    GrowthData _1YearGrowth,
    string? Nationality
) : TwitchData(id: id, follower: follower);
