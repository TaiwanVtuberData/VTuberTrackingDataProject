namespace GenerateJsonFile.Types;

internal record VTuberPopularityData(
    string id,
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
