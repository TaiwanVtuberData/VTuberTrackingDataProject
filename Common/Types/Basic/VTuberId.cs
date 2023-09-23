using LanguageExt;

namespace Common.Types.Basic;

public record VTuberId(string Value) : IComparable<VTuberId> {
    public static Validation<ValidationError, VTuberId> Validate(string rawId) {
        if (rawId.Trim() != rawId) {
            return new ValidationError($"There is leading or trailing whitespce: {rawId}");
        }

        if (rawId.Length != 32) {
            return new ValidationError($"ID should be a valid UUID with lowercase and no '-': {rawId}");
        } else {
            return new VTuberId(rawId);
        }
    }

    public int CompareTo(VTuberId? other) {
        return this.Value.CompareTo(other?.Value);
    }
}
