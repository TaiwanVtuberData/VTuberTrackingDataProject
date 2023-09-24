using Common.Types;
using Common.Types.Basic;

namespace FetchRecord;

internal record VTuberRecord(VTuberId VTuberId, YouTubeRecord YouTube, TwitchStatistics Twitch);
