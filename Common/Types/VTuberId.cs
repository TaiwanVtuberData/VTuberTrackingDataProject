using LanguageExt;

namespace Common.Types;
internal class VTuberId {
  public static Validation<ValidationError, string> Validate(string rawId) {
    if (rawId.Length != 32) {
      return new ValidationError($"ID should be a valid UUID with lowercase and no '-': {rawId}");
    } else {
      return rawId;
    }
  }
}
