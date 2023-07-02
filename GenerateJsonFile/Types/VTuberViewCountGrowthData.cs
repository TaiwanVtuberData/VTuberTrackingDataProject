namespace GenerateJsonFile.Types;

internal record VTuberViewCountGrowthData(
    string id,
    Activity activity,
    string name,
    string? imgUrl,
    YouTubeViewCountGrowthData? YouTube,
    TwitchData? Twitch,
    VideoInfo? popularVideo,
    string? group,
    string? nationality);

internal record VTuberViewCountChangeDataResponse(
    List<VTuberViewCountGrowthData> VTubers);
