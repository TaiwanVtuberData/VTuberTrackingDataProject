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
            VTuberRecord.YouTubeData.YouTubeRecord? lhsRecord = lhs.Value.YouTube?.GetRecord(TargetDateTime);
            VTuberRecord.YouTubeData.YouTubeRecord? rhsRecord = rhs.Value.YouTube?.GetRecord(TargetDateTime);

            ulong lhsYouTubeSub = lhsRecord.HasValue ? lhsRecord.Value.SubscriberCount : 0;
            ulong rhsYouTubeSub = rhsRecord.HasValue ? rhsRecord.Value.SubscriberCount : 0;

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
            VTuberRecord.TwitchData.TwitchRecord? lhsRecord = lhs.Value.Twitch?.GetRecord(TargetDateTime);
            VTuberRecord.TwitchData.TwitchRecord? rhsRecord = rhs.Value.Twitch?.GetRecord(TargetDateTime);

            ulong lhsTwitchFollower = lhsRecord.HasValue ? lhsRecord.Value.FollowerCount : 0;
            ulong rhsTwitchFollower = rhsRecord.HasValue ? rhsRecord.Value.FollowerCount : 0;

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
            VTuberRecord.YouTubeData.YouTubeRecord? lhsYouTubeRecord = lhs.Value.YouTube?.GetRecord(TargetDateTime);
            VTuberRecord.YouTubeData.YouTubeRecord? rhsYouTubeRecord = rhs.Value.YouTube?.GetRecord(TargetDateTime);

            ulong lhsYouTubeSub = lhsYouTubeRecord.HasValue ? lhsYouTubeRecord.Value.SubscriberCount : 0;
            ulong rhsYouTubeSub = rhsYouTubeRecord.HasValue ? rhsYouTubeRecord.Value.SubscriberCount : 0;


            VTuberRecord.TwitchData.TwitchRecord? lhsTwitchRecord = lhs.Value.Twitch?.GetRecord(TargetDateTime);
            VTuberRecord.TwitchData.TwitchRecord? rhsTwitchRecord = rhs.Value.Twitch?.GetRecord(TargetDateTime);

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
            VTuberRecord.YouTubeData.YouTubeRecord? lhsYouTubeRecord = lhs.Value.YouTube?.GetRecord(TargetDateTime);
            VTuberRecord.YouTubeData.YouTubeRecord? rhsYouTubeRecord = rhs.Value.YouTube?.GetRecord(TargetDateTime);

            ulong lhsYouTubeView = lhsYouTubeRecord.HasValue ? lhsYouTubeRecord.Value.RecentMedianViewCount : 0;
            ulong rhsYouTubeView = rhsYouTubeRecord.HasValue ? rhsYouTubeRecord.Value.RecentMedianViewCount : 0;


            VTuberRecord.TwitchData.TwitchRecord? lhsTwitchRecord = lhs.Value.Twitch?.GetRecord(TargetDateTime);
            VTuberRecord.TwitchData.TwitchRecord? rhsTwitchRecord = rhs.Value.Twitch?.GetRecord(TargetDateTime);

            ulong lhsTwitchView = lhsTwitchRecord.HasValue ? lhsTwitchRecord.Value.RecentMedianViewCount : 0;
            ulong rhsTwitchView = rhsTwitchRecord.HasValue ? rhsTwitchRecord.Value.RecentMedianViewCount : 0;


            ulong lhsCombinedView = lhsYouTubeView + lhsTwitchView;
            ulong rhsCombinedView = rhsYouTubeView + rhsTwitchView;

            return lhsCombinedView.CompareTo(rhsCombinedView);
        }
    }
}
