using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace GenerateRecordList.Types;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GrowthRecordType {
    [EnumMember(Value = "none")]
    none,
    [EnumMember(Value = "partial")]
    partial,
    [EnumMember(Value = "full")]
    full,
}
