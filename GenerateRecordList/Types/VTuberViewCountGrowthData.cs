using Common.Types.Basic;
using Common.Utils;
using System.Text.Json.Serialization;

namespace GenerateRecordList.Types;

public record VTuberViewCountGrowthData(
    [property: JsonConverter(typeof(VTuberIdJsonConverter))] VTuberId id,
    Activity activity,
    string name,
    string? imgUrl,
    YouTubeViewCountGrowthData? YouTube,
    TwitchData? Twitch,
    VideoInfo? popularVideo,
    string? group,
    string? nationality);

public record VTuberViewCountChangeDataResponse(
    List<VTuberViewCountGrowthData> VTubers);
