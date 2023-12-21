using LanguageExt;

namespace Common.Types;

public class ValidationError(string e) : NewType<ValidationError, string>(e) {
}
