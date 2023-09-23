using Common.Types.Basic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Common.Utils;
public class VTuberIdJsonConverter : JsonConverter<VTuberId> {
    public override VTuberId? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        string? str = reader.GetString();

        if (str is null) {
            throw new Exception("Input is null.");
        } else {
            return new VTuberId(str);
        }
    }

    public override void Write(Utf8JsonWriter writer, VTuberId value, JsonSerializerOptions options) {
        writer.WriteStringValue(value.Value);
    }
}
