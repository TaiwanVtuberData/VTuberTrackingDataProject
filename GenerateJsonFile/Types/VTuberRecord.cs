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

        public readonly record struct Record(
            ulong SubscriberCount,
            ulong TotalViewCount,
            ulong RecentMedianViewCount,
            ulong HighestViewCount,
            string HighestViewedVideoId);
        private readonly Dictionary<DateTime, Record> DictRecord = new();

        public readonly record struct BasicData(
            ulong SubscriberCount,
            ulong TotalViewCount);
        private readonly Dictionary<DateTime, BasicData> DictBasicData = new();

        public void AddRecord(DateTime recordTime, Record record)
        {
            DictRecord.Add(recordTime, record);
        }

        public void AddBasicData(DateTime recordTime, BasicData basicData)
        {
            DictBasicData.Add(recordTime, basicData);
        }

        public Record? GetRecord(DateTime TargetDateTime)
        {
            if (!DictRecord.ContainsKey(TargetDateTime))
                return null;

            return DictRecord[TargetDateTime];
        }

        public Dictionary<DateTime, Record>.KeyCollection GetRecordDateTimes()
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
        public readonly record struct Record(
            ulong FollowerCount,
            ulong RecentMedianViewCount,
            ulong HighestViewCount,
            string HighestViewedVideoId);
        private readonly Dictionary<DateTime, Record> DictRecord = new();

        public readonly record struct BasicData(
            ulong SubscriberCount,
            ulong TotalViewCount);
        private readonly Dictionary<DateTime, BasicData> DictBasicData = new();

        public void AddRecord(DateTime recordTime, Record record)
        {
            DictRecord.Add(recordTime, record);
        }

        public void AddBasicData(DateTime recordTime, BasicData basicData)
        {
            DictBasicData.Add(recordTime, basicData);
        }


        public Record? GetRecord(DateTime TargetDateTime)
        {
            if (!DictRecord.ContainsKey(TargetDateTime))
                return null;

            return DictRecord[TargetDateTime];
        }
    }
    public TwitchData? Twitch { get; set; }
}
