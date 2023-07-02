using Common.Types;

namespace FetchStatistics;

internal record VTuberRecord(VTuberId VTuberId, YouTubeRecord YouTube, TwitchStatistics Twitch);
