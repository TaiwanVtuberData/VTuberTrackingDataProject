using static Common.Types.YouTubeSubscriberCountToTuple;

namespace Common.Types;

public record YouTubeSubscriberCountToTuple(
    SubscriberCountTo Total,
    SubscriberCountTo Livestream,
    SubscriberCountTo Video
    ) {
    public record SubscriberCountTo(
        decimal MedianViewCount,
        decimal Popularity
        );
}
