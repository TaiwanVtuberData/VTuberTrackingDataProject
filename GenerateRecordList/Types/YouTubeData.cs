namespace GenerateRecordList.Types;

public record YouTubeData(
    string id,
    BaseCountType subscriber)
    : BaseYouTubeData(id);
