using Common.Types.Basic;
using Common.Utils;
using System.Text.Json.Serialization;

namespace GenerateJsonFile.Types;

internal record VTuberGraduateData(
    [property: JsonConverter(typeof(VTuberIdJsonConverter))] VTuberId id,
    Activity activity,
    string name,
    string? imgUrl,
    YouTubeData? YouTube,
    TwitchData? Twitch,
    VideoInfo? popularVideo,
    string? group,
    string? nationality,
    string? debutDate,
    string graduateDate);

internal record VTuberGraduateDataResponse(
    List<VTuberGraduateData> VTubers);
