using Common.Types;
using Common.Types.Basic;

namespace FetchStatistics;

internal record VTuberRecord(VTuberId VTuberId, YouTubeRecord YouTube, TwitchStatistics Twitch);
