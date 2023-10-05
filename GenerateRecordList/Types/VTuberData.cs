using Common.Types.Basic;
using Common.Utils;
using System.Text.Json.Serialization;

namespace GenerateRecordList.Types;

public record VTuberData(
    [property: JsonConverter(typeof(VTuberIdJsonConverter))] VTuberId id,
    Activity activity,
    string name,
    string? imgUrl,
    YouTubeData? YouTube,
    TwitchData? Twitch,
    VideoInfo? popularVideo,
    string? group,
    string? nationality,
    string? debutDate);

public record VTuberDataResponse(
    List<VTuberData> VTubers);
