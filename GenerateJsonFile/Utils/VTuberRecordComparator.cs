using Common.Types.Basic;
using GenerateJsonFile.Types;

namespace GenerateJsonFile.Utils;
internal class VTuberRecordComparator {
    public class YouTubeSubscriberCount : IComparer<KeyValuePair<string, VTuberRecord>> {
        readonly DateTime TargetDateTime;
        public YouTubeSubscriberCount(DateTime targetDateTime) {
            TargetDateTime = targetDateTime;
        }
        public int Compare(KeyValuePair<string, VTuberRecord> lhs, KeyValuePair<string, VTuberRecord> rhs) {
            VTuberRecord.YouTubeData.BasicData? lhsData = lhs.Value.YouTube?.GetBasicData(TargetDateTime);
            VTuberRecord.YouTubeData.BasicData? rhsData = rhs.Value.YouTube?.GetBasicData(TargetDateTime);

            ulong lhsYouTubeSub = lhsData?.SubscriberCount ?? 0;
            ulong rhsYouTubeSub = rhsData?.SubscriberCount ?? 0;

            return lhsYouTubeSub.CompareTo(rhsYouTubeSub);
        }
    }

    public class TwitcheFollowerCount : IComparer<KeyValuePair<string, VTuberRecord>> {
        readonly DateTime TargetDateTime;
        public TwitcheFollowerCount(DateTime targetDateTime) {
            TargetDateTime = targetDateTime;
        }
        public int Compare(KeyValuePair<string, VTuberRecord> lhs, KeyValuePair<string, VTuberRecord> rhs) {
            VTuberRecord.TwitchData.BasicData? lhsData = lhs.Value.Twitch?.GetBasicData(TargetDateTime);
            VTuberRecord.TwitchData.BasicData? rhsData = rhs.Value.Twitch?.GetBasicData(TargetDateTime);

            ulong lhsTwitchFollower = lhsData?.FollowerCount ?? 0;
            ulong rhsTwitchFollower = rhsData?.FollowerCount ?? 0;

            return lhsTwitchFollower.CompareTo(rhsTwitchFollower);
        }
    }

    public class CombinedCount : IComparer<KeyValuePair<VTuberId, VTuberRecord>> {
        readonly DateTime TargetDateTime;
        public CombinedCount(DateTime targetDateTime) {
            TargetDateTime = targetDateTime;
        }
        public int Compare(KeyValuePair<VTuberId, VTuberRecord> lhs, KeyValuePair<VTuberId, VTuberRecord> rhs) {
            VTuberRecord.YouTubeData.BasicData? lhsYouTubeRecord = lhs.Value.YouTube?.GetBasicData(TargetDateTime);
            VTuberRecord.YouTubeData.BasicData? rhsYouTubeRecord = rhs.Value.YouTube?.GetBasicData(TargetDateTime);

            ulong lhsYouTubeSub = lhsYouTubeRecord?.SubscriberCount ?? 0;
            ulong rhsYouTubeSub = rhsYouTubeRecord?.SubscriberCount ?? 0;


            VTuberRecord.TwitchData.BasicData? lhsTwitchRecord = lhs.Value.Twitch?.GetBasicData(TargetDateTime);
            VTuberRecord.TwitchData.BasicData? rhsTwitchRecord = rhs.Value.Twitch?.GetBasicData(TargetDateTime);

            ulong lhsTwitchFollower = lhsTwitchRecord?.FollowerCount ?? 0;
            ulong rhsTwitchFollower = rhsTwitchRecord?.FollowerCount ?? 0;


            ulong lhsCombinedCount = lhsYouTubeSub + lhsTwitchFollower;
            ulong rhsCombinedCount = rhsYouTubeSub + rhsTwitchFollower;

            return lhsCombinedCount.CompareTo(rhsCombinedCount);
        }
    }

    public class CombinedViewCount : IComparer<KeyValuePair<VTuberId, VTuberRecord>> {
        readonly DateTime TargetDateTime;
        public CombinedViewCount(DateTime targetDateTime) {
            TargetDateTime = targetDateTime;
        }
        public int Compare(KeyValuePair<VTuberId, VTuberRecord> lhs, KeyValuePair<VTuberId, VTuberRecord> rhs) {
            VTuberRecord.YouTubeData.Record? lhsYouTubeRecord = lhs.Value.YouTube?.GetRecord(TargetDateTime);
            VTuberRecord.YouTubeData.Record? rhsYouTubeRecord = rhs.Value.YouTube?.GetRecord(TargetDateTime);

            ulong lhsYouTubeView = lhsYouTubeRecord?.RecentTotalMedianViewCount ?? 0;
            ulong rhsYouTubeView = rhsYouTubeRecord?.RecentTotalMedianViewCount ?? 0;

            VTuberRecord.TwitchData.Record? lhsTwitchRecord = lhs.Value.Twitch?.GetRecord(TargetDateTime);
            VTuberRecord.TwitchData.Record? rhsTwitchRecord = rhs.Value.Twitch?.GetRecord(TargetDateTime);

            ulong lhsTwitchView = lhsTwitchRecord?.RecentMedianViewCount ?? 0;
            ulong rhsTwitchView = rhsTwitchRecord?.RecentMedianViewCount ?? 0;

            ulong lhsCombinedView = lhsYouTubeView + lhsTwitchView;
            ulong rhsCombinedView = rhsYouTubeView + rhsTwitchView;

            return lhsCombinedView.CompareTo(rhsCombinedView);
        }
    }
}
