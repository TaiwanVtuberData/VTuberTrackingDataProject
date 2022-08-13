using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace GenerateJsonFile.Types;

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum Activity {
  [EnumMember(Value = "preparing")]
  preparing,
  [EnumMember(Value = "active")]
  active,
  [EnumMember(Value = "graduate")]
  graduate,
}
