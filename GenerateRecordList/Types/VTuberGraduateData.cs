using Common.Types.Basic;
using Common.Utils;
using System.Text.Json.Serialization;

namespace GenerateRecordList.Types;

public record VTuberGraduateData(
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

public record VTuberGraduateDataResponse(
    List<VTuberGraduateData> VTubers);
