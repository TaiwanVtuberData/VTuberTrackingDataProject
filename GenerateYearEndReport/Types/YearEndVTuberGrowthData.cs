using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Common.Types.Basic;
using Common.Utils;
using GenerateRecordList.Types;

namespace GenerateYearEndReport.Types;

public record YearEndVTuberGrowthData(
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

public record YearEndVTuberGrowthDataResponse(List<YearEndVTuberGrowthData> VTubers);
