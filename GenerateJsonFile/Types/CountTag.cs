using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace GenerateJsonFile.Types;

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum CountTag {
  [EnumMember(Value = "has")]
  has,
  [EnumMember(Value = "hidden")]
  hidden,
  [EnumMember(Value = "no")]
  no,
}
