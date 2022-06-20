using GenerateJsonFile.Types;

namespace GenerateJsonFile.Utils;
internal class VTuberRecordComparator
{
    public class YouTubeSubscriberCount : IComparer<KeyValuePair<string, VTuberRecord>>
    {
        readonly DateTime TargetDateTime;
        public YouTubeSubscriberCount(DateTime targetDateTime)
        {
            TargetDateTime = targetDateTime;
        }
        public int Compare(KeyValuePair<string, VTuberRecord> lhs, KeyValuePair<string, VTuberRecord> rhs)
        {
            VTuberRecord.YouTubeData.BasicData? lhsData = lhs.Value.YouTube?.GetBasicData(TargetDateTime);
            VTuberRecord.YouTubeData.BasicData? rhsData = rhs.Value.YouTube?.GetBasicData(TargetDateTime);

            ulong lhsYouTubeSub = lhsData.HasValue ? lhsData.Value.SubscriberCount : 0;
            ulong rhsYouTubeSub = rhsData.HasValue ? rhsData.Value.SubscriberCount : 0;

            return lhsYouTubeSub.CompareTo(rhsYouTubeSub);
        }
    }

    public class TwitcheFollowerCount : IComparer<KeyValuePair<string, VTuberRecord>>
    {
        readonly DateTime TargetDateTime;
        public TwitcheFollowerCount(DateTime targetDateTime)
        {
            TargetDateTime = targetDateTime;
        }
        public int Compare(KeyValuePair<string, VTuberRecord> lhs, KeyValuePair<string, VTuberRecord> rhs)
        {
            VTuberRecord.TwitchData.BasicData? lhsData = lhs.Value.Twitch?.GetBasicData(TargetDateTime);
            VTuberRecord.TwitchData.BasicData? rhsData = rhs.Value.Twitch?.GetBasicData(TargetDateTime);

            ulong lhsTwitchFollower = lhsData.HasValue ? lhsData.Value.FollowerCount : 0;
            ulong rhsTwitchFollower = rhsData.HasValue ? rhsData.Value.FollowerCount : 0;

            return lhsTwitchFollower.CompareTo(rhsTwitchFollower);
        }
    }

    public class CombinedCount : IComparer<KeyValuePair<string, VTuberRecord>>
    {
        readonly DateTime TargetDateTime;
        public CombinedCount(DateTime targetDateTime)
        {
            TargetDateTime = targetDateTime;
        }
        public int Compare(KeyValuePair<string, VTuberRecord> lhs, KeyValuePair<string, VTuberRecord> rhs)
        {
            VTuberRecord.YouTubeData.BasicData? lhsYouTubeRecord = lhs.Value.YouTube?.GetBasicData(TargetDateTime);
            VTuberRecord.YouTubeData.BasicData? rhsYouTubeRecord = rhs.Value.YouTube?.GetBasicData(TargetDateTime);

            ulong lhsYouTubeSub = lhsYouTubeRecord.HasValue ? lhsYouTubeRecord.Value.SubscriberCount : 0;
            ulong rhsYouTubeSub = rhsYouTubeRecord.HasValue ? rhsYouTubeRecord.Value.SubscriberCount : 0;


            VTuberRecord.TwitchData.BasicData? lhsTwitchRecord = lhs.Value.Twitch?.GetBasicData(TargetDateTime);
            VTuberRecord.TwitchData.BasicData? rhsTwitchRecord = rhs.Value.Twitch?.GetBasicData(TargetDateTime);

            ulong lhsTwitchFollower = lhsTwitchRecord.HasValue ? lhsTwitchRecord.Value.FollowerCount : 0;
            ulong rhsTwitchFollower = rhsTwitchRecord.HasValue ? rhsTwitchRecord.Value.FollowerCount : 0;


            ulong lhsCombinedCount = lhsYouTubeSub + lhsTwitchFollower;
            ulong rhsCombinedCount = rhsYouTubeSub + rhsTwitchFollower;

            return lhsCombinedCount.CompareTo(rhsCombinedCount);
        }
    }

    public class CombinedViewCount : IComparer<KeyValuePair<string, VTuberRecord>>
    {
        readonly DateTime TargetDateTime;
        public CombinedViewCount(DateTime targetDateTime)
        {
            TargetDateTime = targetDateTime;
        }
        public int Compare(KeyValuePair<string, VTuberRecord> lhs, KeyValuePair<string, VTuberRecord> rhs)
        {
            VTuberRecord.YouTubeData.Record? lhsYouTubeRecord = lhs.Value.YouTube?.GetRecord(TargetDateTime);
            VTuberRecord.YouTubeData.Record? rhsYouTubeRecord = rhs.Value.YouTube?.GetRecord(TargetDateTime);

            ulong lhsYouTubeView = lhsYouTubeRecord.HasValue ? lhsYouTubeRecord.Value.RecentMedianViewCount : 0;
            ulong rhsYouTubeView = rhsYouTubeRecord.HasValue ? rhsYouTubeRecord.Value.RecentMedianViewCount : 0;


            VTuberRecord.TwitchData.Record? lhsTwitchRecord = lhs.Value.Twitch?.GetRecord(TargetDateTime);
            VTuberRecord.TwitchData.Record? rhsTwitchRecord = rhs.Value.Twitch?.GetRecord(TargetDateTime);

            ulong lhsTwitchView = lhsTwitchRecord.HasValue ? lhsTwitchRecord.Value.RecentMedianViewCount : 0;
            ulong rhsTwitchView = rhsTwitchRecord.HasValue ? rhsTwitchRecord.Value.RecentMedianViewCount : 0;


            ulong lhsCombinedView = lhsYouTubeView + lhsTwitchView;
            ulong rhsCombinedView = rhsYouTubeView + rhsTwitchView;

            return lhsCombinedView.CompareTo(rhsCombinedView);
        }
    }
}
