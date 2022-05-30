using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace GenerateJsonFile.Types;

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum GrowthRecordType
{
    [EnumMember(Value = "none")]
    none,
    [EnumMember(Value = "partial")]
    partial,
    [EnumMember(Value = "full")]
    full,
}
