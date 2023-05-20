namespace GenerateJsonFile.Types;

internal readonly record struct VTuberGraduateData(
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
    string graduateDate);

internal readonly record struct VTuberGraduateDataResponse(
    List<VTuberGraduateData> VTubers);
