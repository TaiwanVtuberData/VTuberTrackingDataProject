using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace GenerateRecordList.Types;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Activity {
    [EnumMember(Value = "preparing")]
    preparing,
    [EnumMember(Value = "active")]
    active,
    [EnumMember(Value = "graduate")]
    graduate,
}
