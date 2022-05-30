namespace GenerateJsonFile.Types;

internal readonly record struct VTuberViewCountGrowthData(
    string id,
    Activity activity,
    string name,
    string? imgUrl,
    YouTubeViewCountGrowthData? YouTube,
    TwitchData? Twitch,
    VideoInfo? popularVideo,
    string? group,
    string? nationality);

internal readonly record struct VTuberViewCountChangeDataResponse(
    List<VTuberViewCountGrowthData> VTubers);
