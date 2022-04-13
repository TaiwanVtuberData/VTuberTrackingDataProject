using LanguageExt;

namespace Common;

public class ValidationError : NewType<ValidationError, string>
{
    public ValidationError(string e) : base(e) { }
}
