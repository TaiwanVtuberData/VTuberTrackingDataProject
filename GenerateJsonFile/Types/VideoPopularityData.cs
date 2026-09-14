using System.Text.Json.Serialization;
using Common.Types.Basic;
using Common.Utils;

namespace GenerateJsonFile.Types;

public record VideoPopularityData(
    [property: JsonConverter(typeof(VTuberIdJsonConverter))] VTuberId id,
    string name,
    string? imgUrl,
    string title,
    string videoUrl,
    string thumbnailUrl,
    ulong viewCount,
    string uploadTime
);

public record VideoPopularityDataResponse(List<VideoPopularityData> videos);
