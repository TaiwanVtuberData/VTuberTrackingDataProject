using Common.Types.Basic;
using Common.Utils;
using System.Text.Json.Serialization;

namespace GenerateJsonFile.Types;

internal record VTuberFullData(
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
    string? graduateDate,
    List<VTuberLivestreamData> livestreams);

internal record SingleVTuberFullDataResponse(
    VTuberFullData VTuber);
