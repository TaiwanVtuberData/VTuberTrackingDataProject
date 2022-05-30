namespace GenerateJsonFile.Types;
public class VTuberRecord
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public DateOnly? DebutDate { get; set; }
    public DateOnly? GraduationDate { get; set; }
    public Common.Types.Activity Activity { get; set; } = Common.Types.Activity.Active;
    public string? GroupName { get; set; }
    public string Nationality { get; set; } = "";
    public string? ImageUrl { get; set; }

    // YouTube
    public class YouTubeData
    {
        public string ChannelId { get; set; } = "";
        public bool hasValidRecord = false;

        public readonly record struct YouTubeRecord(
            ulong SubscriberCount,
            ulong TotalViewCount,
            ulong RecentMedianViewCount,
            ulong HighestViewCount,
            string HighestViewedVideoId);
        private readonly Dictionary<DateTime, YouTubeRecord> DictRecord = new();

        public void AddRecord(DateTime recordTime, YouTubeRecord record)
        {
            DictRecord.Add(recordTime, record);
        }

        public YouTubeRecord? GetRecord(DateTime TargetDateTime)
        {
            if (!DictRecord.ContainsKey(TargetDateTime))
                return null;

            return DictRecord[TargetDateTime];
        }

        public Dictionary<DateTime, YouTubeRecord>.KeyCollection GetDateTimes()
        {
            return DictRecord.Keys;
        }
    }
    public YouTubeData? YouTube { get; set; }

    public class TwitchData
    {
        public string ChannelId { get; set; } = "";
        public string ChannelName { get; set; } = "";
        public bool hasValidRecord = false;
        public readonly record struct TwitchRecord(
            ulong FollowerCount,
            ulong RecentMedianViewCount,
            ulong HighestViewCount,
            string HighestViewedVideoId);
        private Dictionary<DateTime, TwitchRecord> DictRecord = new();
        private DateTime LatestDateTime = DateTime.MinValue;

        public void AddRecord(DateTime recordTime, TwitchRecord record)
        {
            LatestDateTime = Max(recordTime, LatestDateTime);
            DictRecord.Add(recordTime, record);
        }

        public TwitchRecord? GetRecord(DateTime TargetDateTime)
        {
            if (!DictRecord.ContainsKey(TargetDateTime))
                return null;

            return DictRecord[LatestDateTime];
        }
    }
    public TwitchData? Twitch { get; set; }

    // Why is that C# doesn't have generic Max function
    private static DateTime Max(DateTime a, DateTime b)
    {
        return (DateTime.Compare(a, b) > 0) ? a : b;
    }
}
