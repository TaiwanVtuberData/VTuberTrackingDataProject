using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace GenerateJsonFile.Types;

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum VideoType
{
    [EnumMember(Value = "YouTube")]
    YouTube,
    [EnumMember(Value = "Twitch")]
    Twitch,
}
internal readonly record struct VideoInfo(VideoType type, string id);
