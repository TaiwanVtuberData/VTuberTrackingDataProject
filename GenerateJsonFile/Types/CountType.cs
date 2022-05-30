namespace GenerateJsonFile.Types;
internal abstract record BaseCountType(CountTag tag);

internal record HasCountType(ulong count): BaseCountType(CountTag.has);
internal record HiddenCountType() : BaseCountType(CountTag.hidden);
internal record NoCountType() : BaseCountType(CountTag.no);
