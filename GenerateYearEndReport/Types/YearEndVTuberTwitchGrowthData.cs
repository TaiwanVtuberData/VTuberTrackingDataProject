using System.Text.Json.Serialization;
using Common.Types.Basic;
using Common.Utils;
using GenerateRecordList.Types;

namespace GenerateYearEndReport.Types;

public record YearEndVTuberTwitchGrowthData(
    [property: JsonConverter(typeof(VTuberIdJsonConverter))] VTuberId id,
    Activity activity,
    string name,
    string? imgUrl,
    YouTubeData? YouTube,
    YearEndTwitchGrowthData? Twitch,
    string? group,
    string? nationality,
    string? debutDate
);

public record YearEndVTuberTwitchGrowthDataResponse(List<YearEndVTuberTwitchGrowthData> VTubers);
