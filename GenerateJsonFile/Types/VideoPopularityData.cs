namespace GenerateJsonFile.Types;

readonly record struct VideoPopularityData(string id, string name, string? imgUrl, string title, string videoUrl, string thumbnailUrl, ulong viewCount, string uploadTime);
