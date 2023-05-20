namespace GenerateJsonFile.Types;

internal readonly record struct VTuberData(
    string id,
    Activity activity,
    string name,
    string? imgUrl,
    YouTubeData? YouTube,
    TwitchData? Twitch,
    VideoInfo? popularVideo,
    string? group,
    string? nationality,
    string? debutDate);

internal readonly record struct VTuberDataResponse(
    List<VTuberData> VTubers);
