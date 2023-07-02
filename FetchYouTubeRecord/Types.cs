using Common.Types;
using System.Collections.Immutable;

namespace FetchYouTubeStatistics;

internal record YouTubeVideoId(string Value);

internal record IdRequstString(string Value);

internal record ChannelRecentViewRecord(
    ImmutableDictionary<YouTubeChannelId, YouTubeRecord.RecentRecordTuple> DictRecentRecordTuple,
    TopVideosList TopVideosList,
    LiveVideosList LiveVideosList
    );