namespace GenerateJsonFile.Types;

internal readonly record struct VTuberPopularityData(
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

readonly record struct VTuberPopularityDataResponse(
    List<VTuberPopularityData> VTubers);
