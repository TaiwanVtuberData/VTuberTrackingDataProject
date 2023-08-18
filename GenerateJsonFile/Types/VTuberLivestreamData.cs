namespace GenerateJsonFile.Types;

internal record VTuberLivestreamData(
    string? title,
    string videoUrl,
    string? thumbnailUrl,
    string? startTime);
