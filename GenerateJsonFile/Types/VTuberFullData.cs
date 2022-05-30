namespace GenerateJsonFile.Types;

internal record VTuberFullData(
    string id,
    Activity activity,
    string name,
    string? imgUrl,
    YouTubeData? YouTube,
    TwitchData? Twitch,
    VideoInfo? popularVideo,
    string? group,
    string? nationality,
    string? debutDate,
    string? graduateDate);

readonly record struct SingleVTuberFullDataResponse(
    VTuberFullData VTuber);
