namespace GenerateJsonFile.Types;

internal record YouTubeData(
    string id,
    BaseCountType subscriber)
    : BaseYouTubeData(id);
