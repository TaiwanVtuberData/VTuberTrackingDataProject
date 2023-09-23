using Common.Types.Basic;
using Common.Utils;
using System.Text.Json.Serialization;

namespace GenerateJsonFile.Types;

internal record VTuberPopularityData(
    [property: JsonConverter(typeof(VTuberIdJsonConverter))] VTuberId id,
    Activity activity,
    string name,
    string? imgUrl,
    YouTubePopularityData? YouTube,
    TwitchPopularityData? Twitch,
    VideoInfo? popularVideo,
    string? group,
    string? nationality,
    string? debutDate);

internal record VTuberPopularityDataResponse(
    List<VTuberPopularityData> VTubers);
