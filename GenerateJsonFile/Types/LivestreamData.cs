namespace GenerateJsonFile.Types;

internal readonly record struct LivestreamData(
    string id,
    string name,
    string? imgUrl,
    string title,
    string videoUrl,
    string? thumbnailUrl,
    string? startTime);

internal readonly record struct LivestreamDataResponse(
    List<LivestreamData> livestreams);
