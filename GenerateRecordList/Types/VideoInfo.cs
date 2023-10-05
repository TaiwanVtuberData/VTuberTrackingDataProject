using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace GenerateRecordList.Types;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum VideoType {
    [EnumMember(Value = "YouTube")]
    YouTube,
    [EnumMember(Value = "Twitch")]
    Twitch,
}
public record VideoInfo(VideoType type, string id);
