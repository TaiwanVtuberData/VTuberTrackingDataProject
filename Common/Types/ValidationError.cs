using LanguageExt;

namespace Common.Types;

public class ValidationError : NewType<ValidationError, string>
{
    public ValidationError(string e) : base(e) { }
}
