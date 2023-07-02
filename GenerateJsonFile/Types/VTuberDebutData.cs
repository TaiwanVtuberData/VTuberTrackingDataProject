namespace GenerateJsonFile.Types;

internal record VTuberDebutData(
    string id,
    Activity activity,
    string name,
    string? imgUrl,
    YouTubeData? YouTube,
    TwitchData? Twitch,
    VideoInfo? popularVideo,
    string? group,
    string? nationality,
    string debutDate);

internal record VTuberDebutDataResponse(
    List<VTuberDebutData> VTubers);
