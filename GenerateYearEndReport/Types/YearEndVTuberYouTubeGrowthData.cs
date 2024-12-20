using System.Text.Json.Serialization;
using Common.Types.Basic;
using Common.Utils;
using GenerateRecordList.Types;

namespace GenerateYearEndReport.Types;

public record YearEndVTuberYouTubeGrowthData(
    [property: JsonConverter(typeof(VTuberIdJsonConverter))] VTuberId id,
    Activity activity,
    string name,
    string? imgUrl,
    YearEndYouTubeGrowthData? YouTube,
    TwitchData? Twitch,
    string? group,
    string? nationality,
    string? debutDate
);

public record YearEndVTuberYouTubeGrowthDataResponse(List<YearEndVTuberYouTubeGrowthData> VTubers);
