using static Common.Types.YouTubeRecord;

namespace Common.Types;

public record YouTubeRecord(
    BasicRecord Basic,
    RecentRecordTuple Recent
    ) {
    public record BasicRecord(
        YouTubeChannelId ChannelId,
        ulong SubscriberCount,
        ulong ViewCount
        );

    public record RecentRecordTuple(
        RecentRecord Total,
        RecentRecord LiveStream,
        RecentRecord Video
    );

    public record RecentRecord(
        ulong MedialViewCount,
        ulong Popularity,
        ulong HighestViewCount,
        string HighestViewdUrl
        );
}
