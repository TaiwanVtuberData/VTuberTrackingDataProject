using Common.Types.Basic;
using GenerateRecordList.Types;

namespace GenerateRecordList.Utils;

public class VTuberRecordComparator
{
    public class YouTubeSubscriberCount(DateTime targetDateTime)
        : IComparer<KeyValuePair<string, VTuberRecord>>
    {
        readonly DateTime TargetDateTime = targetDateTime;

        public int Compare(
            KeyValuePair<string, VTuberRecord> lhs,
            KeyValuePair<string, VTuberRecord> rhs
        )
        {
            VTuberRecord.YouTubeData.BasicData? lhsData = lhs.Value.YouTube?.GetBasicDataOrLatest(
                TargetDateTime
            );
            VTuberRecord.YouTubeData.BasicData? rhsData = rhs.Value.YouTube?.GetBasicDataOrLatest(
                TargetDateTime
            );

            ulong lhsYouTubeSub = lhsData?.SubscriberCount ?? 0;
            ulong rhsYouTubeSub = rhsData?.SubscriberCount ?? 0;

            return lhsYouTubeSub.CompareTo(rhsYouTubeSub);
        }
    }

    public class TwitcheFollowerCount(DateTime targetDateTime)
        : IComparer<KeyValuePair<string, VTuberRecord>>
    {
        readonly DateTime TargetDateTime = targetDateTime;

        public int Compare(
            KeyValuePair<string, VTuberRecord> lhs,
            KeyValuePair<string, VTuberRecord> rhs
        )
        {
            VTuberRecord.TwitchData.BasicData? lhsData = lhs.Value.Twitch?.GetBasicDataOrLatest(
                TargetDateTime
            );
            VTuberRecord.TwitchData.BasicData? rhsData = rhs.Value.Twitch?.GetBasicDataOrLatest(
                TargetDateTime
            );

            ulong lhsTwitchFollower = lhsData?.FollowerCount ?? 0;
            ulong rhsTwitchFollower = rhsData?.FollowerCount ?? 0;

            return lhsTwitchFollower.CompareTo(rhsTwitchFollower);
        }
    }

    public class CombinedCount(DateTimeOffset targetDateTime)
        : IComparer<KeyValuePair<VTuberId, VTuberRecord>>
    {
        readonly DateTimeOffset TargetDateTime = targetDateTime;

        public int Compare(
            KeyValuePair<VTuberId, VTuberRecord> lhs,
            KeyValuePair<VTuberId, VTuberRecord> rhs
        )
        {
            VTuberRecord.YouTubeData.BasicData? lhsYouTubeRecord =
                lhs.Value.YouTube?.GetBasicDataOrLatest(TargetDateTime);
            VTuberRecord.YouTubeData.BasicData? rhsYouTubeRecord =
                rhs.Value.YouTube?.GetBasicDataOrLatest(TargetDateTime);

            ulong lhsYouTubeSub = lhsYouTubeRecord?.SubscriberCount ?? 0;
            ulong rhsYouTubeSub = rhsYouTubeRecord?.SubscriberCount ?? 0;

            VTuberRecord.TwitchData.BasicData? lhsTwitchRecord =
                lhs.Value.Twitch?.GetBasicDataOrLatest(TargetDateTime);
            VTuberRecord.TwitchData.BasicData? rhsTwitchRecord =
                rhs.Value.Twitch?.GetBasicDataOrLatest(TargetDateTime);

            ulong lhsTwitchFollower = lhsTwitchRecord?.FollowerCount ?? 0;
            ulong rhsTwitchFollower = rhsTwitchRecord?.FollowerCount ?? 0;

            ulong lhsCombinedCount = lhsYouTubeSub + lhsTwitchFollower;
            ulong rhsCombinedCount = rhsYouTubeSub + rhsTwitchFollower;

            return lhsCombinedCount.CompareTo(rhsCombinedCount);
        }
    }

    public class CombinedViewCount(DateTime targetDateTime)
        : IComparer<KeyValuePair<VTuberId, VTuberRecord>>
    {
        readonly DateTime TargetDateTime = targetDateTime;

        public int Compare(
            KeyValuePair<VTuberId, VTuberRecord> lhs,
            KeyValuePair<VTuberId, VTuberRecord> rhs
        )
        {
            VTuberRecord.YouTubeData.Record? lhsYouTubeRecord =
                lhs.Value.YouTube?.GetRecordOrLatest(TargetDateTime);
            VTuberRecord.YouTubeData.Record? rhsYouTubeRecord =
                rhs.Value.YouTube?.GetRecordOrLatest(TargetDateTime);

            ulong lhsYouTubeView = lhsYouTubeRecord?.RecentTotalMedianViewCount ?? 0;
            ulong rhsYouTubeView = rhsYouTubeRecord?.RecentTotalMedianViewCount ?? 0;

            VTuberRecord.TwitchData.Record? lhsTwitchRecord = lhs.Value.Twitch?.GetRecordOrLatest(
                TargetDateTime
            );
            VTuberRecord.TwitchData.Record? rhsTwitchRecord = rhs.Value.Twitch?.GetRecordOrLatest(
                TargetDateTime
            );

            ulong lhsTwitchView = lhsTwitchRecord?.RecentMedianViewCount ?? 0;
            ulong rhsTwitchView = rhsTwitchRecord?.RecentMedianViewCount ?? 0;

            ulong lhsCombinedView = lhsYouTubeView + lhsTwitchView;
            ulong rhsCombinedView = rhsYouTubeView + rhsTwitchView;

            return lhsCombinedView.CompareTo(rhsCombinedView);
        }
    }
}
