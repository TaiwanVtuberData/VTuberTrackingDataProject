namespace Common.Types;
public class VTuberStatistics
{
    // V1
    // Name,SubscriberCount,ViewCount,MedianViewCount,HighestViewCount
    // V2
    // Name,SubscriberCount,ViewCount,MedianViewCount,HighestViewCount,HighestViewedVideoURL
    // V3
    // Display Name,YouTube Subscriber Count,YouTube View Count,YouTube Recent Median View Count,YouTube Recent Highest View Count,YouTube Recent Highest Viewed Video URL,Twitch Follower Count,Twitch Recent Median View Count,Twitch Recent Highest View Count,Twitch Recent Highest Viewed Video URL
    public enum Version
    {
        Unknown,
        V1,
        V2,
        V3,
    }

    public static Version GetVersionByHeaderLength(int headerLength)
    {
        return headerLength switch
        {
            5 => Version.V1,
            6 => Version.V2,
            10 => Version.V3,
            _ => Version.Unknown,
        };
    }

    public string Id { get; private set; } = "";
    public YouTubeStatistics YouTube { get; private set; } = new();
    public TwitchStatistics Twitch { get; private set; } = new();
    // Combined
    public ulong CombinedRecentMedianViewCount { get; private set; } = 0;

    public VTuberStatistics(string id)
    {
        this.Id = id;
    }

    public VTuberStatistics(string id, YouTubeStatistics youTubeStatistics, TwitchStatistics twitchStatistics)
    {
        this.Id = id;
        this.YouTube = youTubeStatistics;
        this.Twitch = twitchStatistics;
    }
    public VTuberStatistics(string[] stringBlocks)
    {
        Version version = GetVersionByHeaderLength(stringBlocks.Length);
        switch (version)
        {
            case Version.V1:
                {
                    // V1
                    // Name,SubscriberCount,ViewCount,MedianViewCount,HighestViewCount
                    this.Id = stringBlocks[0];

                    this.YouTube = new YouTubeStatistics
                    {
                        SubscriberCount = ulong.Parse(stringBlocks[1]),
                        ViewCount = ulong.Parse(stringBlocks[2]),
                        RecentMedianViewCount = ulong.Parse(stringBlocks[3]),
                        RecentHighestViewCount = ulong.Parse(stringBlocks[4]),
                    };
                }
                break;
            case Version.V2:
                {
                    // V2
                    // Name,SubscriberCount,ViewCount,MedianViewCount,HighestViewCount,HighestViewedVideoURL
                    this.Id = stringBlocks[0];

                    this.YouTube = new YouTubeStatistics
                    {
                        SubscriberCount = ulong.Parse(stringBlocks[1]),
                        ViewCount = ulong.Parse(stringBlocks[2]),
                        RecentMedianViewCount = ulong.Parse(stringBlocks[3]),
                        RecentHighestViewCount = ulong.Parse(stringBlocks[4]),
                        HighestViewedVideoURL = stringBlocks[5],
                    };
                }
                break;

            case Version.V3:
                {
                    // V3
                    // Display Name,YouTube Subscriber Count,YouTube View Count,YouTube Recent Median View Count,YouTube Recent Highest View Count,YouTube Recent Highest Viewed Video URL,Twitch Follower Count,Twitch Recent Median View Count,Twitch Recent Highest View Count,Twitch Recent Highest Viewed Video URL
                    this.Id = stringBlocks[0];

                    this.YouTube = new YouTubeStatistics
                    {
                        SubscriberCount = ulong.Parse(stringBlocks[1]),
                        ViewCount = ulong.Parse(stringBlocks[2]),
                        RecentMedianViewCount = ulong.Parse(stringBlocks[3]),
                        RecentHighestViewCount = ulong.Parse(stringBlocks[4]),
                        HighestViewedVideoURL = stringBlocks[5],
                    };

                    this.Twitch = new TwitchStatistics
                    {
                        FollowerCount = ulong.Parse(stringBlocks[6]),
                        RecentMedianViewCount = ulong.Parse(stringBlocks[7]),
                        RecentHighestViewCount = ulong.Parse(stringBlocks[8]),
                        HighestViewedVideoURL = stringBlocks[9],
                    };
                }
                break;
            case Version.Unknown:
                throw new Exception("Unknown CSV version.");
        }

        this.CombinedRecentMedianViewCount = this.YouTube.RecentMedianViewCount + this.Twitch.RecentMedianViewCount;

        this.YouTube.UpdateSubscriberCountToMedianViewCount();
        this.Twitch.UpdateFollowerCountToMedianViewCount();
    }

    public VTuberStatistics(string[] stringBlocks, string displayName)
    {
        Version version = GetVersionByHeaderLength(stringBlocks.Length);
        switch (version)
        {
            case Version.V1:
                {
                    // V1
                    // Name,SubscriberCount,ViewCount,MedianViewCount,HighestViewCount
                    this.Id = displayName;

                    this.YouTube = new YouTubeStatistics
                    {
                        SubscriberCount = ulong.Parse(stringBlocks[1]),
                        ViewCount = ulong.Parse(stringBlocks[2]),
                        RecentMedianViewCount = ulong.Parse(stringBlocks[3]),
                        RecentHighestViewCount = ulong.Parse(stringBlocks[4]),
                    };
                }
                break;
            case Version.V2:
                {
                    // V2
                    // Name,SubscriberCount,ViewCount,MedianViewCount,HighestViewCount,HighestViewedVideoURL
                    this.Id = displayName;

                    this.YouTube = new YouTubeStatistics
                    {
                        SubscriberCount = ulong.Parse(stringBlocks[1]),
                        ViewCount = ulong.Parse(stringBlocks[2]),
                        RecentMedianViewCount = ulong.Parse(stringBlocks[3]),
                        RecentHighestViewCount = ulong.Parse(stringBlocks[4]),
                        HighestViewedVideoURL = stringBlocks[5],
                    };
                }
                break;

            case Version.V3:
                {
                    // V3
                    // Display Name,YouTube Subscriber Count,YouTube View Count,YouTube Recent Median View Count,YouTube Recent Highest View Count,YouTube Recent Highest Viewed Video URL,Twitch Follower Count,Twitch Recent Median View Count,Twitch Recent Highest View Count,Twitch Recent Highest Viewed Video URL
                    this.Id = displayName;

                    this.YouTube = new YouTubeStatistics
                    {
                        SubscriberCount = ulong.Parse(stringBlocks[1]),
                        ViewCount = ulong.Parse(stringBlocks[2]),
                        RecentMedianViewCount = ulong.Parse(stringBlocks[3]),
                        RecentHighestViewCount = ulong.Parse(stringBlocks[4]),
                        HighestViewedVideoURL = stringBlocks[5],
                    };

                    this.Twitch = new TwitchStatistics
                    {
                        FollowerCount = ulong.Parse(stringBlocks[6]),
                        RecentMedianViewCount = ulong.Parse(stringBlocks[7]),
                        RecentHighestViewCount = ulong.Parse(stringBlocks[8]),
                        HighestViewedVideoURL = stringBlocks[9],
                    };
                }
                break;
            case Version.Unknown:
                throw new Exception("Unknown CSV version.");
        }

        this.CombinedRecentMedianViewCount = this.YouTube.RecentMedianViewCount + this.Twitch.RecentMedianViewCount;

        this.YouTube.UpdateSubscriberCountToMedianViewCount();
        this.Twitch.UpdateFollowerCountToMedianViewCount();
    }

    public void Add(string[] stringBlocks)
    {
        Version version = GetVersionByHeaderLength(stringBlocks.Length);

        // V1
        // Name,SubscriberCount,ViewCount,MedianViewCount,HighestViewCount
        // V2
        // Name,SubscriberCount,ViewCount,MedianViewCount,HighestViewCount,HighestViewedVideoURL
        // V3
        // Display Name,YouTube Subscriber Count,YouTube View Count,YouTube Recent Median View Count,YouTube Recent Highest View Count,YouTube Recent Highest Viewed Video URL,Twitch Follower Count,Twitch Recent Median View Count,Twitch Recent Highest View Count,Twitch Recent Highest Viewed Video URL

        switch (version)
        {
            case Version.V1:
                {
                    this.YouTube.SubscriberCount += ulong.Parse(stringBlocks[1]);
                    this.YouTube.ViewCount += ulong.Parse(stringBlocks[2]);
                    this.YouTube.RecentMedianViewCount += ulong.Parse(stringBlocks[3]);

                    if (ulong.Parse(stringBlocks[4]) > this.YouTube.RecentHighestViewCount)
                    {
                        this.YouTube.RecentHighestViewCount = ulong.Parse(stringBlocks[4]);
                    }

                    this.YouTube.UpdateSubscriberCountToMedianViewCount();
                }
                break;
            case Version.V2:
                {
                    this.YouTube.SubscriberCount += ulong.Parse(stringBlocks[1]);
                    this.YouTube.ViewCount += ulong.Parse(stringBlocks[2]);
                    this.YouTube.RecentMedianViewCount += ulong.Parse(stringBlocks[3]);

                    if (ulong.Parse(stringBlocks[4]) > this.YouTube.RecentHighestViewCount)
                    {
                        this.YouTube.RecentHighestViewCount = ulong.Parse(stringBlocks[4]);
                        this.YouTube.HighestViewedVideoURL = stringBlocks[5];
                    }

                    this.YouTube.UpdateSubscriberCountToMedianViewCount();
                }
                break;

            case Version.V3:
                {
                    this.YouTube.SubscriberCount += ulong.Parse(stringBlocks[1]);
                    this.YouTube.ViewCount += ulong.Parse(stringBlocks[2]);
                    this.YouTube.RecentMedianViewCount += ulong.Parse(stringBlocks[3]);

                    if (ulong.Parse(stringBlocks[4]) > this.YouTube.RecentHighestViewCount)
                    {
                        this.YouTube.RecentHighestViewCount = ulong.Parse(stringBlocks[4]);
                        this.YouTube.HighestViewedVideoURL = stringBlocks[5];
                    }

                    this.YouTube.UpdateSubscriberCountToMedianViewCount();

                    this.Twitch.FollowerCount += ulong.Parse(stringBlocks[6]);
                    this.Twitch.RecentMedianViewCount += ulong.Parse(stringBlocks[7]);

                    if (ulong.Parse(stringBlocks[8]) > this.Twitch.RecentHighestViewCount)
                    {
                        this.Twitch.RecentHighestViewCount = ulong.Parse(stringBlocks[8]);
                        this.Twitch.HighestViewedVideoURL = stringBlocks[9];
                    }

                    this.Twitch.UpdateFollowerCountToMedianViewCount();
                }
                break;
            case Version.Unknown:
                throw new Exception("The version is unknown.");
        }

        this.CombinedRecentMedianViewCount = this.YouTube.RecentMedianViewCount + this.Twitch.RecentMedianViewCount;

        this.YouTube.UpdateSubscriberCountToMedianViewCount();
        this.Twitch.UpdateFollowerCountToMedianViewCount();
    }

    public static VTuberStatistics GenerateStatisticsByInterpolation(decimal preRatio, VTuberStatistics preStat, VTuberStatistics postStat)
    {
        decimal postRatio = 1m - preRatio;

        VTuberStatistics rValue = new(preStat.Id);
        rValue.YouTube.SubscriberCount = (ulong)(preRatio * preStat.YouTube.SubscriberCount + postRatio * postStat.YouTube.SubscriberCount);
        rValue.YouTube.ViewCount = (ulong)(preRatio * preStat.YouTube.ViewCount + postRatio * postStat.YouTube.ViewCount);
        rValue.YouTube.RecentMedianViewCount = (ulong)(preRatio * preStat.YouTube.RecentMedianViewCount + postRatio * postStat.YouTube.RecentMedianViewCount);
        rValue.YouTube.RecentHighestViewCount = (ulong)(preRatio * preStat.YouTube.RecentHighestViewCount + postRatio * postStat.YouTube.RecentHighestViewCount);

        rValue.YouTube.UpdateSubscriberCountToMedianViewCount();

        rValue.Twitch.FollowerCount = (ulong)(preRatio * preStat.Twitch.FollowerCount + postRatio * postStat.Twitch.FollowerCount);
        rValue.Twitch.RecentMedianViewCount = (ulong)(preRatio * preStat.Twitch.RecentMedianViewCount + postRatio * postStat.Twitch.RecentMedianViewCount);
        rValue.Twitch.RecentHighestViewCount = (ulong)(preRatio * preStat.Twitch.RecentHighestViewCount + postRatio * postStat.Twitch.RecentHighestViewCount);

        rValue.Twitch.UpdateFollowerCountToMedianViewCount();

        rValue.CombinedRecentMedianViewCount = rValue.YouTube.RecentMedianViewCount + rValue.Twitch.RecentMedianViewCount;

        return rValue;
    }
}