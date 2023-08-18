namespace GenerateJsonFile.Types;

internal record LivestreamData(
    string id,
    string name,
    string? imgUrl,
    string? title,
    string videoUrl,
    string? thumbnailUrl,
    string? startTime);

internal record LivestreamDataResponse(
    List<LivestreamData> livestreams);
