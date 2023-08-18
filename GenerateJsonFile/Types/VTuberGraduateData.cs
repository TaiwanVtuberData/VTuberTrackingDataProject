namespace GenerateJsonFile.Types;

internal record VTuberGraduateData(
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

internal record VTuberGraduateDataResponse(
    List<VTuberGraduateData> VTubers);
