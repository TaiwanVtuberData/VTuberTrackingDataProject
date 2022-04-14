using System.Text.Json.Serialization;

namespace GenerateJsonFile.Types;

internal readonly record struct VTuberData(
    string id,
    Activity activity,
    string name,
    string? imgUrl,
    YouTubeData? YouTube,
    TwitchData? Twitch,
    VideoInfo? popularVideo,
    string? group,
    string? nationality);
