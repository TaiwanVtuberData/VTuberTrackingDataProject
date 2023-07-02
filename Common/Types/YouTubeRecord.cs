using static Common.Types.YouTubeRecord;

namespace Common.Types;

public record YouTubeRecord(
    BasicRecord Basic,
    RecentRecord RecentTotal
    //RecentRecord RecentLiveStream,
    //RecentRecord RecentVideo
    ) {
    public record BasicRecord(
        YouTubeChannelId ChannelId,
        ulong SubscriberCount,
        ulong ViewCount
        );
    public record RecentRecord(
        ulong MedialViewCount,
        ulong Popularity,
        ulong HighestViewCount,
        string HighestViewdUrl
        );
}
