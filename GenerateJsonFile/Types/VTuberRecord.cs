namespace GenerateJsonFile.Types;
public class VTuberRecord
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public DateTime? DebutDate { get; set; }
    public DateTime? GraduationDate { get; set; }
    public Common.Types.Activity Activity { get; set; } = Common.Types.Activity.Active;
    public string GroupName { get; set; } = "";
    public string Nationality { get; set; } = "";
    public string ThumbnailUrl { get; set; } = "";

    // YouTube
    public class YouTubeData
    {
        public string ChannelId { get; set; } = "";

        public class YouTubeRecord
        {
            public ulong SubscriberCount { get; set; } = 0;
            public ulong TotalViewCount { get; set; } = 0;
            public ulong RecentMedianViewCount { get; set; } = 0;
            public ulong HighestViewCount { get; set; } = 0;
            public string HighestViewedVideoId { get; set; } = "";
        }
        public Dictionary<DateTime, YouTubeRecord> DictRecord = new();

        public bool HasRecord()
        {
            return DictRecord.Count > 0;
        }
        public ulong GetLatestTotalViewCount(DateTime TargetDateTime)
        {
            if (!DictRecord.ContainsKey(TargetDateTime))
                return 0;
            return DictRecord[DictRecord.Keys.Max()].TotalViewCount;
        }
        public ulong GetLatestSubscriberCount(DateTime TargetDateTime)
        {
            if (!DictRecord.ContainsKey(TargetDateTime))
                return 0;
            return DictRecord[DictRecord.Keys.Max()].SubscriberCount;
        }
        public ulong GetLatestRecentMedianViewCount(DateTime TargetDateTime)
        {
            if (!DictRecord.ContainsKey(TargetDateTime))
                return 0;
            return DictRecord[DictRecord.Keys.Max()].RecentMedianViewCount;
        }
        public ulong GetLatestHighestViewCount(DateTime TargetDateTime)
        {
            if (!DictRecord.ContainsKey(TargetDateTime))
                return 0;
            return DictRecord[DictRecord.Keys.Max()].HighestViewCount;
        }
    }
    public YouTubeData YouTube { get; set; } = new();

    public class TwitchData
    {
        public string ChannelId { get; set; } = "";
        public string ChannelName { get; set; } = "";
        public class TwitchRecord
        {
            public ulong FollowerCount { get; set; } = 0;
            public ulong RecentMedianViewCount { get; set; } = 0;
            public ulong HighestViewCount { get; set; } = 0;
            public string HighestViewedVideoId { get; set; } = "";
        }
        public Dictionary<DateTime, TwitchRecord> DictRecord = new();

        public bool HasRecord()
        {
            return DictRecord.Count > 0;
        }
        public ulong GetLatestFollowerCount(DateTime TargetDateTime)
        {
            if (!DictRecord.ContainsKey(TargetDateTime))
                return 0;
            return DictRecord[DictRecord.Keys.Max()].FollowerCount;
        }
        public ulong GetLatestRecentMedianViewCount(DateTime TargetDateTime)
        {
            if (!DictRecord.ContainsKey(TargetDateTime))
                return 0;
            return DictRecord[DictRecord.Keys.Max()].RecentMedianViewCount;
        }
        public ulong GetLatestHighestViewCount(DateTime TargetDateTime)
        {
            if (!DictRecord.ContainsKey(TargetDateTime))
                return 0;
            return DictRecord[DictRecord.Keys.Max()].HighestViewCount;
        }
    }
    public TwitchData Twitch { get; set; } = new();
}
