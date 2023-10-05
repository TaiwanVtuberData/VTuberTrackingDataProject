using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace GenerateRecordList.Types;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CountTag {
    [EnumMember(Value = "has")]
    has,
    [EnumMember(Value = "hidden")]
    hidden,
    [EnumMember(Value = "no")]
    no,
}
