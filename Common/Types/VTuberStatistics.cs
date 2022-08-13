namespace Common.Types;
public class VTuberStatistics {
  // V1
  // Name,SubscriberCount,ViewCount,MedianViewCount,HighestViewCount
  // V2
  // Name,SubscriberCount,ViewCount,MedianViewCount,HighestViewCount,HighestViewedVideoURL
  // V3
  // Display Name,YouTube Subscriber Count,YouTube View Count,YouTube Recent Median View Count,YouTube Recent Highest View Count,YouTube Recent Highest Viewed Video URL,Twitch Follower Count,Twitch Recent Median View Count,Twitch Recent Highest View Count,Twitch Recent Highest Viewed Video URL
  public enum Version {
    Unknown,
    V1,
    V2,
    V3,
    V4,
  }

  public static Version GetVersionByHeaderLength(int headerLength) {
    return headerLength switch {
      5 => Version.V1,
      6 => Version.V2,
      10 => Version.V3,
      12 => Version.V4,
      _ => Version.Unknown,
    };
  }

  public string Id { get; private set; } = "";
  public YouTubeStatistics YouTube { get; private set; } = new();
  public TwitchStatistics Twitch { get; private set; } = new();
  // Combined
  public ulong CombinedRecentMedianViewCount { get; private set; } = 0;
  public ulong CombinedPopularity { get; private set; } = 0;

  public VTuberStatistics(string id) {
    this.Id = id;
  }

  public VTuberStatistics(string id, YouTubeStatistics youTubeStatistics, TwitchStatistics twitchStatistics) {
    this.Id = id;
    this.YouTube = youTubeStatistics;
    this.Twitch = twitchStatistics;
  }

  public VTuberStatistics(string[] stringBlocks, string? displayName = null) {
    // if displayName is not specified, use ID from stringBlocks
    this.Id = displayName ?? stringBlocks[0];

    Version version = GetVersionByHeaderLength(stringBlocks.Length);
    switch (version) {
      case Version.V1: {
          // V1
          // Name,
          // SubscriberCount,ViewCount,MedianViewCount,HighestViewCount

          this.YouTube = new YouTubeStatistics {
            ViewCount = ulong.Parse(stringBlocks[2]),
            RecentMedianViewCount = ulong.Parse(stringBlocks[3]),
            RecentHighestViewCount = ulong.Parse(stringBlocks[4]),
          };
          this.YouTube.UpdateSubscriberCount(ulong.Parse(stringBlocks[1]));
        }
        break;
      case Version.V2: {
          // V2
          // Name,
          // SubscriberCount,ViewCount,MedianViewCount,HighestViewCount,HighestViewedVideoURL

          this.YouTube = new YouTubeStatistics {
            ViewCount = ulong.Parse(stringBlocks[2]),
            RecentMedianViewCount = ulong.Parse(stringBlocks[3]),
            RecentHighestViewCount = ulong.Parse(stringBlocks[4]),
            HighestViewedVideoURL = stringBlocks[5],
          };
          this.YouTube.UpdateSubscriberCount(ulong.Parse(stringBlocks[1]));
        }
        break;
      case Version.V3: {
          // V3
          // Display Name (or ID),
          // YouTube Subscriber Count,YouTube View Count,YouTube Recent Median View Count,YouTube Recent Highest View Count,YouTube Recent Highest Viewed Video URL,
          // Twitch Follower Count,Twitch Recent Median View Count,Twitch Recent Highest View Count,Twitch Recent Highest Viewed Video URL

          this.YouTube = new YouTubeStatistics {
            ViewCount = ulong.Parse(stringBlocks[2]),
            RecentMedianViewCount = ulong.Parse(stringBlocks[3]),
            RecentHighestViewCount = ulong.Parse(stringBlocks[4]),
            HighestViewedVideoURL = stringBlocks[5],
          };
          this.YouTube.UpdateSubscriberCount(ulong.Parse(stringBlocks[1]));

          this.Twitch = new TwitchStatistics {
            RecentMedianViewCount = ulong.Parse(stringBlocks[7]),
            RecentHighestViewCount = ulong.Parse(stringBlocks[8]),
            HighestViewedVideoURL = stringBlocks[9],
          };
          this.Twitch.UpdateFollowerCount(ulong.Parse(stringBlocks[6]));
        }
        break;
      case Version.V4: {
          // V4
          // ID,
          // YouTube Subscriber Count,YouTube View Count,YouTube Recent Median View Count,YouTube Recent Popularity,YouTube Recent Highest View Count,YouTube Recent Highest Viewed Video URL,
          // Twitch Follower Count,Twitch Recent Median View Count,Twitch Recent Popularity,Twitch Recent Highest View Count,Twitch Recent Highest Viewed Video URL

          this.YouTube = new YouTubeStatistics {
            ViewCount = ulong.Parse(stringBlocks[2]),
            RecentMedianViewCount = ulong.Parse(stringBlocks[3]),
            RecentPopularity = ulong.Parse(stringBlocks[4]),
            RecentHighestViewCount = ulong.Parse(stringBlocks[5]),
            HighestViewedVideoURL = stringBlocks[6],
          };
          this.YouTube.UpdateSubscriberCount(ulong.Parse(stringBlocks[1]));

          this.Twitch = new TwitchStatistics {
            RecentMedianViewCount = ulong.Parse(stringBlocks[8]),
            RecentPopularity = ulong.Parse(stringBlocks[9]),
            RecentHighestViewCount = ulong.Parse(stringBlocks[10]),
            HighestViewedVideoURL = stringBlocks[11],
          };
          this.Twitch.UpdateFollowerCount(ulong.Parse(stringBlocks[7]));
        }
        break;
      case Version.Unknown:
        throw new Exception("Unknown CSV version.");
    }

    UpdateCombinedValue();
  }

  public void Add(string[] stringBlocks) {
    Version version = GetVersionByHeaderLength(stringBlocks.Length);

    switch (version) {
      case Version.V1: {
          this.YouTube.ViewCount += ulong.Parse(stringBlocks[2]);
          this.YouTube.RecentMedianViewCount += ulong.Parse(stringBlocks[3]);
          this.YouTube.UpdateSubscriberCount(this.YouTube.SubscriberCount + ulong.Parse(stringBlocks[1]));

          if (ulong.Parse(stringBlocks[4]) > this.YouTube.RecentHighestViewCount) {
            this.YouTube.RecentHighestViewCount = ulong.Parse(stringBlocks[4]);
          }
        }
        break;
      case Version.V2: {
          this.YouTube.ViewCount += ulong.Parse(stringBlocks[2]);
          this.YouTube.RecentMedianViewCount += ulong.Parse(stringBlocks[3]);
          this.YouTube.UpdateSubscriberCount(this.YouTube.SubscriberCount + ulong.Parse(stringBlocks[1]));

          if (ulong.Parse(stringBlocks[4]) > this.YouTube.RecentHighestViewCount) {
            this.YouTube.RecentHighestViewCount = ulong.Parse(stringBlocks[4]);
            this.YouTube.HighestViewedVideoURL = stringBlocks[5];
          }
        }
        break;

      case Version.V3: {
          this.YouTube.ViewCount += ulong.Parse(stringBlocks[2]);
          this.YouTube.RecentMedianViewCount += ulong.Parse(stringBlocks[3]);
          this.YouTube.UpdateSubscriberCount(this.YouTube.SubscriberCount + ulong.Parse(stringBlocks[1]));

          if (ulong.Parse(stringBlocks[4]) > this.YouTube.RecentHighestViewCount) {
            this.YouTube.RecentHighestViewCount = ulong.Parse(stringBlocks[4]);
            this.YouTube.HighestViewedVideoURL = stringBlocks[5];
          }

          this.Twitch.RecentMedianViewCount += ulong.Parse(stringBlocks[7]);
          this.Twitch.UpdateFollowerCount(this.Twitch.FollowerCount + ulong.Parse(stringBlocks[6]));

          if (ulong.Parse(stringBlocks[8]) > this.Twitch.RecentHighestViewCount) {
            this.Twitch.RecentHighestViewCount = ulong.Parse(stringBlocks[8]);
            this.Twitch.HighestViewedVideoURL = stringBlocks[9];
          }
        }
        break;

      case Version.V4: {
          this.YouTube.ViewCount += ulong.Parse(stringBlocks[2]);
          this.YouTube.RecentMedianViewCount += ulong.Parse(stringBlocks[3]);
          this.YouTube.RecentPopularity = ulong.Parse(stringBlocks[4]);
          this.YouTube.UpdateSubscriberCount(this.YouTube.SubscriberCount + ulong.Parse(stringBlocks[1]));

          if (ulong.Parse(stringBlocks[5]) > this.YouTube.RecentHighestViewCount) {
            this.YouTube.RecentHighestViewCount = ulong.Parse(stringBlocks[5]);
            this.YouTube.HighestViewedVideoURL = stringBlocks[6];
          }

          this.Twitch.RecentMedianViewCount += ulong.Parse(stringBlocks[8]);
          this.Twitch.RecentPopularity += ulong.Parse(stringBlocks[9]);
          this.Twitch.UpdateFollowerCount(this.Twitch.FollowerCount + ulong.Parse(stringBlocks[7]));

          if (ulong.Parse(stringBlocks[10]) > this.Twitch.RecentHighestViewCount) {
            this.Twitch.RecentHighestViewCount = ulong.Parse(stringBlocks[10]);
            this.Twitch.HighestViewedVideoURL = stringBlocks[11];
          }
        }
        break;
      case Version.Unknown:
        throw new Exception("The version is unknown.");
    }

    UpdateCombinedValue();
  }

  private void UpdateCombinedValue() {
    this.CombinedRecentMedianViewCount = this.YouTube.RecentMedianViewCount + this.Twitch.RecentMedianViewCount;
    this.CombinedPopularity = this.YouTube.RecentPopularity + this.Twitch.RecentPopularity;
  }

  public static VTuberStatistics GenerateStatisticsByInterpolation(decimal preRatio, VTuberStatistics preStat, VTuberStatistics postStat) {
    decimal postRatio = 1m - preRatio;

    VTuberStatistics rValue = new(preStat.Id);
    rValue.YouTube.ViewCount = (ulong)(preRatio * preStat.YouTube.ViewCount + postRatio * postStat.YouTube.ViewCount);
    rValue.YouTube.RecentMedianViewCount = (ulong)(preRatio * preStat.YouTube.RecentMedianViewCount + postRatio * postStat.YouTube.RecentMedianViewCount);
    rValue.YouTube.RecentPopularity = (ulong)(preRatio * preStat.YouTube.RecentPopularity + postRatio * postStat.YouTube.RecentPopularity);
    rValue.YouTube.RecentHighestViewCount = (ulong)(preRatio * preStat.YouTube.RecentHighestViewCount + postRatio * postStat.YouTube.RecentHighestViewCount);
    rValue.YouTube.UpdateSubscriberCount((ulong)(preRatio * preStat.YouTube.SubscriberCount + postRatio * postStat.YouTube.SubscriberCount));

    rValue.Twitch.RecentMedianViewCount = (ulong)(preRatio * preStat.Twitch.RecentMedianViewCount + postRatio * postStat.Twitch.RecentMedianViewCount);
    rValue.Twitch.RecentPopularity = (ulong)(preRatio * preStat.Twitch.RecentPopularity + postRatio * postStat.Twitch.RecentPopularity);
    rValue.Twitch.RecentHighestViewCount = (ulong)(preRatio * preStat.Twitch.RecentHighestViewCount + postRatio * postStat.Twitch.RecentHighestViewCount);
    rValue.Twitch.UpdateFollowerCount((ulong)(preRatio * preStat.Twitch.FollowerCount + postRatio * postStat.Twitch.FollowerCount));

    rValue.CombinedRecentMedianViewCount = rValue.YouTube.RecentMedianViewCount + rValue.Twitch.RecentMedianViewCount;

    rValue.UpdateCombinedValue();

    return rValue;
  }
}