namespace Common.Types;
public class YouTubeStatistics
{
    public ulong SubscriberCount { get; set; } = 0;
    public ulong ViewCount { get; set; } = 0;
    public ulong RecentMedianViewCount { get; set; } = 0;
    public ulong RecentHighestViewCount { get; set; } = 0;
    public string HighestViewedVideoURL { get; set; } = "";
    public decimal SubscriberCountToMedianViewCount { get; set; } = 0m;

    public YouTubeStatistics()
    {
        UpdateSubscriberCountToMedianViewCount();
    }

    public void UpdateSubscriberCountToMedianViewCount()
    {
        if (SubscriberCount == 0)
        {
            SubscriberCountToMedianViewCount = 0m;
        }
        else
        {
            SubscriberCountToMedianViewCount = (decimal)RecentMedianViewCount / SubscriberCount * 100m;
        }
    }
}
