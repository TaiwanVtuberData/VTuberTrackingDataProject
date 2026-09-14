using System.Collections.Immutable;
using Common.Types;

namespace FetchYouTubeRecord;

internal record YouTubeVideoId(string Value);

internal record IdRequstString(string Value);

internal record ChannelRecentViewRecord(
    ImmutableDictionary<YouTubeChannelId, YouTubeRecord.RecentRecordTuple> DictRecentRecordTuple,
    TopVideosList TopVideosList,
    LiveVideosList LiveVideosList
);
