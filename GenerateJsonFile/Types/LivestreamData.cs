using Common.Types.Basic;
using Common.Utils;
using System.Text.Json.Serialization;

namespace GenerateJsonFile.Types;

internal record LivestreamData(
    [property: JsonConverter(typeof(VTuberIdJsonConverter))] VTuberId id,
    string name,
    string? imgUrl,
    string? title,
    string videoUrl,
    string? thumbnailUrl,
    string? startTime);

internal record LivestreamDataResponse(
    List<LivestreamData> livestreams);
