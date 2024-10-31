using LanguageExt;

namespace GenerateAdvertisement.Types;

public class ValidationError(string e) : NewType<ValidationError, string>(e) {
}
