namespace Common.Types;

public record YouTubeChannelId(string Value);

public record CommonStatistics(
    decimal MedianViewCount,
    decimal Popularity
    );