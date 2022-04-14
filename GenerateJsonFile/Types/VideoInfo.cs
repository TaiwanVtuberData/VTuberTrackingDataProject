using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace GenerateJsonFile.Types;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum VideoType
{
    [EnumMember(Value = "YouTube")]
    YouTube,
    [EnumMember(Value = "Twitch")]
    Twitch,
}
readonly record struct VideoInfo(VideoType type, string id);
