using GenerateRecordList.Types;

namespace GenerateYearEndReport.Types;

public record YearEndTwitchGrowthData(
    string id,
    BaseCountType follower,
    GrowthData _365DaysGrowth,
    string? Nationality
) : TwitchData(id: id, follower: follower);
