namespace Common.Types;
public class YouTubeStatistics
{
    public ulong SubscriberCount { get; private set; } = 0;
    public ulong ViewCount { get; set; } = 0;
    public ulong RecentMedianViewCount { get; set; } = 0;
    public ulong RecentHighestViewCount { get; set; } = 0;
    public ulong RecentPopularity { get; set; } = 0;
    public string HighestViewedVideoURL { get; set; } = "";
    public decimal SubscriberCountToMedianViewCount { get; private set; } = 0m;
    public decimal SubscriberCountToPopularity { get; private set; } = 0m;

    public YouTubeStatistics()
    {
        UpdateSubscriberCount(0);
    }

    public void UpdateSubscriberCount(ulong subscriberCount)
    {
        SubscriberCount = subscriberCount;

        if (SubscriberCount == 0)
        {
            SubscriberCountToMedianViewCount = 0m;
            SubscriberCountToPopularity = 0m;
        }
        else
        {
            SubscriberCountToMedianViewCount = (decimal)RecentMedianViewCount / SubscriberCount * 100m;
            SubscriberCountToPopularity = (decimal)RecentPopularity / SubscriberCount * 100m;
        }
    }
}
