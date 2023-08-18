using static Common.Types.YouTubeRecord;

namespace Common.Types;

public record YouTubeRecord(
    BasicRecord Basic,
    RecentRecordTuple Recent
    ) {
    public record BasicRecord(
        ulong SubscriberCount,
        ulong ViewCount
        );

    public record RecentRecordTuple(
        RecentRecord Total,
        RecentRecord Livestream,
        RecentRecord Video
    );

    public record RecentRecord(
        ulong MedialViewCount,
        ulong Popularity,
        ulong HighestViewCount,
        string HighestViewdUrl
        );
}
