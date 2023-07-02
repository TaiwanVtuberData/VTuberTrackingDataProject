namespace GenerateJsonFile.Types;

internal record VTuberData(
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

internal record VTuberDataResponse(
    List<VTuberData> VTubers);
