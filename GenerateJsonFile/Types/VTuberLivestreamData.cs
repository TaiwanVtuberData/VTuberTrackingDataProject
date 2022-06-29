namespace GenerateJsonFile.Types;

internal readonly record struct VTuberLivestreamData(
    string? title,
    string videoUrl,
    string? thumbnailUrl,
    string? startTime);
