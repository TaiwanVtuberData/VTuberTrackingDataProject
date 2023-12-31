using Common.Types.Basic;

namespace GenerateRecordList.Types;
public class VTuberRecord {
    public VTuberId Id { get; set; } = new VTuberId("");
    public string DisplayName { get; set; } = "";
    public DateOnly? DebutDate { get; set; }
    public DateOnly? GraduationDate { get; set; }
    public Common.Types.Activity Activity { get; set; } = Common.Types.Activity.Active;
    public string? GroupName { get; set; }
    public string Nationality { get; set; } = "";
    public string? ImageUrl { get; set; }

    // YouTube
    public class YouTubeData {
        public string ChannelId { get; set; } = "";
        public bool hasValidRecord = false;

        public record Record(
            ulong SubscriberCount,
            ulong TotalViewCount,
            ulong RecentTotalMedianViewCount,
            ulong RecentLivestreamMedianViewCount,
            ulong RecentVideoMedianViewCount,
            ulong RecentPopularity,
            ulong HighestViewCount,
            string HighestViewedVideoId);
        private readonly Dictionary<DateTime, Record> DictRecord = [];

        public record BasicData(
            ulong SubscriberCount,
            ulong TotalViewCount);
        private readonly Dictionary<DateTime, BasicData> DictBasicData = [];

        public void AddRecord(DateTime recordTime, Record record) {
            DictRecord.Add(recordTime, record);
        }

        public void AddBasicData(DateTime recordTime, BasicData basicData) {
            DictBasicData.Add(recordTime, basicData);
        }

        public Record? GetRecord(DateTime TargetDateTime) {
            if (DictRecord.TryGetValue(TargetDateTime, out Record? value)) {
                return value;
            } else {
                return null;
            }
        }

        public Dictionary<DateTime, BasicData>.KeyCollection GetBasicDataDateTimes() {
            return DictBasicData.Keys;
        }

        public BasicData? GetBasicData(DateTime TargetDateTime) {
            if (DictBasicData.TryGetValue(TargetDateTime, out BasicData? value)) {
                return value;
            } else {
                return null;
            }
        }
    }
    public YouTubeData? YouTube { get; set; }

    public class TwitchData {
        public string ChannelId { get; set; } = "";
        public string ChannelName { get; set; } = "";
        public bool hasValidRecord = false;
        public record Record(
            ulong FollowerCount,
            ulong RecentMedianViewCount,
            ulong RecentPopularity,
            ulong HighestViewCount,
            string HighestViewedVideoId);
        private readonly Dictionary<DateTime, Record> DictRecord = [];

        public record BasicData(
            ulong FollowerCount);
        private readonly Dictionary<DateTime, BasicData> DictBasicData = [];

        public void AddRecord(DateTime recordTime, Record record) {
            DictRecord.Add(recordTime, record);
        }

        public void AddBasicData(DateTime recordTime, BasicData basicData) {
            DictBasicData.Add(recordTime, basicData);
        }

        public Record? GetRecord(DateTime TargetDateTime) {
            if (DictRecord.TryGetValue(TargetDateTime, out Record? value)) {
                return value;
            } else {
                return null;
            }
        }

        public BasicData? GetBasicData(DateTime TargetDateTime) {
            if (DictBasicData.TryGetValue(TargetDateTime, out BasicData? value)) {
                return value;
            } else {
                return null;
            }
        }
    }
    public TwitchData? Twitch { get; set; }
}
