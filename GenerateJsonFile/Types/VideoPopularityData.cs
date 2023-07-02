namespace GenerateJsonFile.Types;

internal record VideoPopularityData(
    string id,
    string name,
    string? imgUrl,
    string title,
    string videoUrl,
    string thumbnailUrl,
    ulong viewCount,
    string uploadTime);

internal record VideoPopularityDataResponse(
    List<VideoPopularityData> videos);
