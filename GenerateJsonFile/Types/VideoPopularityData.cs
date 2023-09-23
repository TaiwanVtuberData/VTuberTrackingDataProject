using Common.Types.Basic;
using Common.Utils;
using System.Text.Json.Serialization;

namespace GenerateJsonFile.Types;

internal record VideoPopularityData(
    [property: JsonConverter(typeof(VTuberIdJsonConverter))] VTuberId id,
    string name,
    string? imgUrl,
    string title,
    string videoUrl,
    string thumbnailUrl,
    ulong viewCount,
    string uploadTime);

internal record VideoPopularityDataResponse(
    List<VideoPopularityData> videos);
