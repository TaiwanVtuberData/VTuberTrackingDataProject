namespace GenerateJsonFile.Types;

internal record VTuberGrowthData(
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

internal record VTuberGrowthDataResponse(
    List<VTuberGrowthData> VTubers);
