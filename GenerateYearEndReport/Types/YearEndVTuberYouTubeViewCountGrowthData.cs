using System.Text.Json.Serialization;
using Common.Types.Basic;
using Common.Utils;
using GenerateRecordList.Types;

namespace GenerateYearEndReport.Types;

public record YearEndVTuberYouTubeViewCountGrowthData(
    [property: JsonConverter(typeof(VTuberIdJsonConverter))] VTuberId id,
    Activity activity,
    string name,
    string? imgUrl,
    YearEndYouTubeViewCountGrowthData? YouTube,
    TwitchData? Twitch,
    string? group,
    string? nationality,
    string? debutDate
);

public record YearEndVTuberViewCountChangeDataResponse(
    List<YearEndVTuberYouTubeViewCountGrowthData> VTubers
);
