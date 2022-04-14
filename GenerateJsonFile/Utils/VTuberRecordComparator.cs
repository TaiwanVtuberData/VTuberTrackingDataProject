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
            ulong lhsYouTubeSub = lhs.Value.YouTube.GetLatestSubscriberCount(TargetDateTime);
            ulong rhsYouTubeSub = rhs.Value.YouTube.GetLatestSubscriberCount(TargetDateTime);

            if (lhsYouTubeSub > rhsYouTubeSub)
                return 1;
            else if (lhsYouTubeSub < rhsYouTubeSub)
                return -1;

            ulong lhsYouTubeView = lhs.Value.YouTube.GetLatestRecentMedianViewCount(TargetDateTime);
            ulong rhsYouTubeView = rhs.Value.YouTube.GetLatestRecentMedianViewCount(TargetDateTime);

            if (lhsYouTubeView > rhsYouTubeView)
                return 1;
            else if (lhsYouTubeView < rhsYouTubeView)
                return -1;

            ulong lhsTwitchFollower = lhs.Value.Twitch.GetLatestFollowerCount(TargetDateTime);
            ulong rhsTwitchFollower = rhs.Value.Twitch.GetLatestFollowerCount(TargetDateTime);

            if (lhsTwitchFollower > rhsTwitchFollower)
                return 1;
            else if (lhsTwitchFollower < rhsTwitchFollower)
                return -1;

            ulong lhsTwitchView = lhs.Value.Twitch.GetLatestRecentMedianViewCount(TargetDateTime);
            ulong rhsTwitchView = rhs.Value.Twitch.GetLatestRecentMedianViewCount(TargetDateTime);

            if (lhsTwitchView > rhsTwitchView)
                return 1;
            else if (lhsTwitchView < rhsTwitchView)
                return -1;

            return 0;

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
            ulong lhsTwitchFollower = lhs.Value.Twitch.GetLatestFollowerCount(TargetDateTime);
            ulong rhsTwitchFollower = rhs.Value.Twitch.GetLatestFollowerCount(TargetDateTime);

            if (lhsTwitchFollower > rhsTwitchFollower)
                return 1;
            else if (lhsTwitchFollower < rhsTwitchFollower)
                return -1;

            ulong lhsTwitchView = lhs.Value.Twitch.GetLatestRecentMedianViewCount(TargetDateTime);
            ulong rhsTwitchView = rhs.Value.Twitch.GetLatestRecentMedianViewCount(TargetDateTime);

            if (lhsTwitchView > rhsTwitchView)
                return 1;
            else if (lhsTwitchView < rhsTwitchView)
                return -1;

            ulong lhsYouTubeSub = lhs.Value.YouTube.GetLatestSubscriberCount(TargetDateTime);
            ulong rhsYouTubeSub = rhs.Value.YouTube.GetLatestSubscriberCount(TargetDateTime);

            if (lhsYouTubeSub > rhsYouTubeSub)
                return 1;
            else if (lhsYouTubeSub < rhsYouTubeSub)
                return -1;

            ulong lhsYouTubeView = lhs.Value.YouTube.GetLatestRecentMedianViewCount(TargetDateTime);
            ulong rhsYouTubeView = rhs.Value.YouTube.GetLatestRecentMedianViewCount(TargetDateTime);

            if (lhsYouTubeView > rhsYouTubeView)
                return 1;
            else if (lhsYouTubeView < rhsYouTubeView)
                return -1;

            return 0;
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
            ulong lhsCombinedCount = lhs.Value.YouTube.GetLatestSubscriberCount(TargetDateTime) + lhs.Value.Twitch.GetLatestFollowerCount(TargetDateTime);
            ulong rhsCombinedCount = rhs.Value.YouTube.GetLatestSubscriberCount(TargetDateTime) + rhs.Value.Twitch.GetLatestFollowerCount(TargetDateTime);

            if (lhsCombinedCount > rhsCombinedCount)
                return 1;
            else if (lhsCombinedCount < rhsCombinedCount)
                return -1;

            ulong lhsCombinedView = lhs.Value.YouTube.GetLatestRecentMedianViewCount(TargetDateTime) + lhs.Value.Twitch.GetLatestRecentMedianViewCount(TargetDateTime);
            ulong rhsCombinedView = rhs.Value.YouTube.GetLatestRecentMedianViewCount(TargetDateTime) + rhs.Value.Twitch.GetLatestRecentMedianViewCount(TargetDateTime);

            if (lhsCombinedView > rhsCombinedView)
                return 1;
            else if (lhsCombinedView < rhsCombinedView)
                return -1;

            return 0;

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
            ulong lhsCombinedView = lhs.Value.YouTube.GetLatestRecentMedianViewCount(TargetDateTime) + lhs.Value.Twitch.GetLatestRecentMedianViewCount(TargetDateTime);
            ulong rhsCombinedView = rhs.Value.YouTube.GetLatestRecentMedianViewCount(TargetDateTime) + rhs.Value.Twitch.GetLatestRecentMedianViewCount(TargetDateTime);

            if (lhsCombinedView > rhsCombinedView)
                return 1;
            else if (lhsCombinedView < rhsCombinedView)
                return -1;

            return 0;

        }
    }
}
