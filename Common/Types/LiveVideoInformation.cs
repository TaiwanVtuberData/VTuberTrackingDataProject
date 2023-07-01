using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;

namespace Common.Types;

public class LiveVideoInformation : IComparable<VideoInformation> {
    [Index(0), HeaderPrefix]
    public string Id { get; set; } = "";
    [Index(4)]
    public string Url { get; init; } = "";
    [Index(2)]
    public string Title { get; init; } = "";
    [Index(5)]
    public string ThumbnailUrl { get; init; } = "";
    [Index(3)]
    public DateTimeOffset PublishDateTime { get; init; } = DateTimeOffset.UnixEpoch;
    [Index(1)]
    public LiveVideoType VideoType { set; get; } = LiveVideoType.upcoming;

    public int CompareTo(VideoInformation? that) {
        if (that == null)
            return 1;

        return this.PublishDateTime.CompareTo(that.PublishDateTime);
    }
}

public sealed class LiveVideoInformationMap : ClassMap<LiveVideoInformation> {
    public LiveVideoInformationMap() {
        Map(m => m.Id).Name("VTuber ID");
        Map(m => m.VideoType).Name("Video Type");
        Map(m => m.Title).Name("Title");
        Map(m => m.PublishDateTime).Name("Publish Time");
        Map(m => m.Url).Name("URL");
        Map(m => m.ThumbnailUrl).Name("Thumbnail URL");

        // 2021-12-31T18:58:28Z
        string RFC3339Format = @"yyyy-MM-ddTHH:mm:ssZ";
        Map(m => m.PublishDateTime).TypeConverterOption.Format(RFC3339Format);
    }
}
