namespace GenerateJsonFile.Types;

internal readonly record struct VideoPopularityData(
    string id,
    string name, 
    string? imgUrl, 
    string title, 
    string videoUrl, 
    string thumbnailUrl, 
    ulong viewCount, 
    string uploadTime);

internal readonly record struct VideoPopularityDataResponse(
    List<VideoPopularityData> videos);
