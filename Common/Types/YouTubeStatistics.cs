using static Common.Types.YouTubeRecord;

namespace Common.Types;
public record YouTubeStatistics(BasicRecord Basic, RecentRecordTuple Recent, YouTubeSubscriberCountToTuple SubscriberCountTo) {

    public YouTubeStatistics(YouTubeRecord YouTubeRecord) : this(
            Basic: YouTubeRecord.Basic,
            Recent: YouTubeRecord.Recent,
            SubscriberCountTo: CreateYouTubeSubscriberCountToTuple(
                YouTubeRecord.Basic.SubscriberCount,
                YouTubeRecord.Recent)
    ) { }

    private static YouTubeSubscriberCountToTuple CreateYouTubeSubscriberCountToTuple(
        ulong subscriberCount, RecentRecordTuple recordTuple) {
        if (subscriberCount == 0) {
            return new(
                Total: new(MedianViewCount: 0, Popularity: 0),
                LiveStream: new(MedianViewCount: 0, Popularity: 0),
                Video: new(MedianViewCount: 0, Popularity: 0)
                );
        } else {
            return new(
                Total: CreateSubscriberCountTo(subscriberCount, recordTuple.Total),
                LiveStream: CreateSubscriberCountTo(subscriberCount, recordTuple.LiveStream),
                Video: CreateSubscriberCountTo(subscriberCount, recordTuple.Video)
                );
        }
    }

    private static YouTubeSubscriberCountToTuple.SubscriberCountTo CreateSubscriberCountTo(
        ulong subscriberCount, RecentRecord recentRecord) {
        return new(
            MedianViewCount: (decimal)recentRecord.MedialViewCount / subscriberCount * 100m,
            Popularity: (decimal)recentRecord.Popularity / subscriberCount * 100m
            );
    }
}
