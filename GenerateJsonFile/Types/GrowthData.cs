namespace GenerateJsonFile.Types;

internal readonly record struct GrowthData(
    decimal diff,
    GrowthRecordType recordType
    );
