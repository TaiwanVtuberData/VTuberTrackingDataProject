using Common.Types.Basic;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using static Common.Utils.CsvUtility;

namespace Common.Types;

public class VideoInformation : IComparable<VideoInformation> {
    [Index(0), HeaderPrefix]
    public VTuberId Id { get; set; } = new VTuberId("");
    [Index(4)]
    public string Url { get; init; } = "";
    [Index(2)]
    public string Title { get; init; } = "";
    [Index(5)]
    public string ThumbnailUrl { get; init; } = "";
    [Index(3)]
    public DateTimeOffset PublishDateTime { get; init; } = DateTimeOffset.UnixEpoch;
    [Index(1)]
    public ulong ViewCount { set; get; } = 0;

    public int CompareTo(VideoInformation? that) {
        if (that == null)
            return 1;

        return this.ViewCount.CompareTo(that.ViewCount);
    }
}

public sealed class VideoInformationMap : ClassMap<VideoInformation> {
    public VideoInformationMap() {
        Map(m => m.Id).Name("VTuber ID");
        Map(m => m.ViewCount).Name("View Count");
        Map(m => m.Title).Name("Title");
        Map(m => m.PublishDateTime).Name("Publish Time");
        Map(m => m.Url).Name("URL");
        Map(m => m.ThumbnailUrl).Name("Thumbnail URL");

        Map(m => m.Id).TypeConverter(new VTuberIdConverter());
        // 2021-12-31T18:58:28Z
        string RFC3339Format = @"yyyy-MM-ddTHH:mm:ssZ";
        Map(m => m.PublishDateTime).TypeConverterOption.Format(RFC3339Format);
    }
}
