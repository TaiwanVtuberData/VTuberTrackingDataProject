namespace GenerateJsonFile.Types;

internal readonly record struct VTuberGrowthData(
    string id,
    Activity activity,
    string name,
    string? imgUrl,
    YouTubeGrowthData? YouTube,
    TwitchData? Twitch,
    VideoInfo? popularVideo,
    string? group,
    string? nationality,
    string? debutDate);

internal readonly record struct VTuberGrowthDataResponse(
    List<VTuberGrowthData> VTubers);
