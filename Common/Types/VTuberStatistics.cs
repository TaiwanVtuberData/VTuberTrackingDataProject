using Common.Types.Basic;

namespace Common.Types;
public class VTuberStatistics {
    public enum Version {
        Unknown,
        V1,
        V2,
        V3,
        V4,
        V5,
    }

    public static Version GetVersion(string header, int headerLength) {
        Version version = GetVersionByHeader(header);

        if (version == Version.Unknown) {
            version = GetVersionByHeaderLength(headerLength);
        }

        return version;
    }

    private static Version GetVersionByHeader(string header) {
        // Format: V5##VTuber ID,YouTube Subscriber Count
        string[] splitString = header.Split("##");

        if (splitString.Length == 2) {
            bool success = Enum.TryParse(splitString[0], false, out Version version);

            if (success) {
                return version;
            }
        }

        return Version.Unknown;
    }

    private static Version GetVersionByHeaderLength(int headerLength) {
        return headerLength switch {
            5 => Version.V1,
            6 => Version.V2,
            10 => Version.V3,
            12 => Version.V4,
            _ => Version.Unknown,
        };
    }

    public VTuberId Id { get; private set; } = new VTuberId("");
    public YouTubeStatistics YouTube { get; private set; } = new(
        Basic: new(
            SubscriberCount: 0,
            ViewCount: 0
            ),
        Recent: new(
            Total: new(
                MedialViewCount: 0,
                Popularity: 0,
                HighestViewCount: 0,
                HighestViewdUrl: ""
                ),
            Livestream: new(
                MedialViewCount: 0,
                Popularity: 0,
                HighestViewCount: 0,
                HighestViewdUrl: ""
                ),
            Video: new(
                MedialViewCount: 0,
                Popularity: 0,
                HighestViewCount: 0,
                HighestViewdUrl: ""
                )
            ),
        SubscriberCountTo: new(
            Total: new(MedianViewCount: 0, Popularity: 0),
            Livestream: new(MedianViewCount: 0, Popularity: 0),
            Video: new(MedianViewCount: 0, Popularity: 0)
            )
    );
    public TwitchStatistics Twitch { get; private set; } = new();
    // Combined
    public CommonStatistics CombinedRecentTotalStatistics { get; private set; } = new(
        MedianViewCount: 0,
        Popularity: 0
        );
    public CommonStatistics CombinedRecentLivestreamStatistics { get; private set; } = new(
        MedianViewCount: 0,
        Popularity: 0
        );
    public CommonStatistics CombinedRecentVideoStatistics { get; private set; } = new(
    MedianViewCount: 0,
    Popularity: 0
    );

    public VTuberStatistics(string id) {
        this.Id = new VTuberId(id);
    }

    public VTuberStatistics(string id, YouTubeStatistics youTubeStatistics, TwitchStatistics twitchStatistics) {
        this.Id = new VTuberId(id);
        this.YouTube = youTubeStatistics;
        this.Twitch = twitchStatistics;
    }

    public VTuberStatistics(string[] header, string[] stringBlocks, string? displayName = null) {
        // if displayName is not specified, use ID from stringBlocks
        this.Id = new VTuberId(displayName ?? stringBlocks[0]);

        Version version = GetVersion(header[0], stringBlocks.Length);
        (YouTubeRecord youTubeRecord, TwitchStatistics twitchStatistics) =
            CreateRecord(version, stringBlocks);

        this.YouTube = new(youTubeRecord);
        this.Twitch = twitchStatistics;

        UpdateCombinedValue();
    }

    public void Add(string[] header, string[] stringBlocks) {
        Version version = GetVersion(header[0], stringBlocks.Length);
        (YouTubeRecord youTubeRecord, TwitchStatistics twitchStatistics) =
            CreateRecord(version, stringBlocks);

        this.YouTube = CombineYouTubeRecord(this.YouTube, youTubeRecord);
        this.Twitch = CombineTwitchStatistics(this.Twitch, twitchStatistics);

        UpdateCombinedValue();
    }

    public static VTuberStatistics GenerateStatisticsByInterpolation(decimal preRatio, VTuberStatistics preStat, VTuberStatistics postStat) {
        decimal postRatio = 1m - preRatio;

        VTuberStatistics rValue = new(preStat.Id.Value) {
            YouTube = CombineYouTubeStatistics(preRatio, preStat.YouTube, postStat.YouTube)
        };

        rValue.Twitch.RecentMedianViewCount = (ulong)(preRatio * preStat.Twitch.RecentMedianViewCount + postRatio * postStat.Twitch.RecentMedianViewCount);
        rValue.Twitch.RecentPopularity = (ulong)(preRatio * preStat.Twitch.RecentPopularity + postRatio * postStat.Twitch.RecentPopularity);
        rValue.Twitch.RecentHighestViewCount = (ulong)(preRatio * preStat.Twitch.RecentHighestViewCount + postRatio * postStat.Twitch.RecentHighestViewCount);
        rValue.Twitch.UpdateFollowerCount((ulong)(preRatio * preStat.Twitch.FollowerCount + postRatio * postStat.Twitch.FollowerCount));

        rValue.UpdateCombinedValue();

        return rValue;
    }

    private static (YouTubeRecord, TwitchStatistics) CreateRecord(Version version, string[] stringBlocks) {
        switch (version) {
            case Version.V1: {
                    // V1
                    // Name,
                    // SubscriberCount,ViewCount,MedianViewCount,HighestViewCount
                    YouTubeRecord youTubeRecord = new(
                        Basic: new(
                            SubscriberCount: ulong.Parse(stringBlocks[1]),
                            ViewCount: ulong.Parse(stringBlocks[2])
                            ),
                        Recent: new(
                            Total: new(
                                MedialViewCount: ulong.Parse(stringBlocks[3]),
                                Popularity: 0,
                                HighestViewCount: ulong.Parse(stringBlocks[4]),
                                HighestViewdUrl: ""
                                ),
                            Livestream: new(
                                MedialViewCount: 0,
                                Popularity: 0,
                                HighestViewCount: 0,
                                HighestViewdUrl: ""
                                ),
                            Video: new(
                                MedialViewCount: 0,
                                Popularity: 0,
                                HighestViewCount: 0,
                                HighestViewdUrl: ""
                                )
                            )
                        );

                    return (youTubeRecord, new TwitchStatistics());
                }
            case Version.V2: {
                    // V2
                    // Name,
                    // SubscriberCount,ViewCount,MedianViewCount,HighestViewCount,HighestViewedVideoURL

                    YouTubeRecord youTubeRecord = new(
                        Basic: new(
                            SubscriberCount: ulong.Parse(stringBlocks[1]),
                            ViewCount: ulong.Parse(stringBlocks[2])
                            ),
                        Recent: new(
                            Total: new(
                                MedialViewCount: ulong.Parse(stringBlocks[3]),
                                Popularity: 0,
                                HighestViewCount: ulong.Parse(stringBlocks[4]),
                                HighestViewdUrl: stringBlocks[5]
                                ),
                            Livestream: new(
                                MedialViewCount: 0,
                                Popularity: 0,
                                HighestViewCount: 0,
                                HighestViewdUrl: ""
                                ),
                            Video: new(
                                MedialViewCount: 0,
                                Popularity: 0,
                                HighestViewCount: 0,
                                HighestViewdUrl: ""
                                )
                            )
                        );
                    return (youTubeRecord, new TwitchStatistics());
                }
            case Version.V3: {
                    // V3
                    // Display Name (or ID),
                    // YouTube Subscriber Count,YouTube View Count,YouTube Recent Median View Count,YouTube Recent Highest View Count,YouTube Recent Highest Viewed Video URL,
                    // Twitch Follower Count,Twitch Recent Median View Count,Twitch Recent Highest View Count,Twitch Recent Highest Viewed Video URL
                    YouTubeRecord youTubeRecord = new(
                        Basic: new(
                            SubscriberCount: ulong.Parse(stringBlocks[1]),
                            ViewCount: ulong.Parse(stringBlocks[2])
                            ),
                        Recent: new(
                            Total: new(
                                MedialViewCount: ulong.Parse(stringBlocks[3]),
                                Popularity: 0,
                                HighestViewCount: ulong.Parse(stringBlocks[4]),
                                HighestViewdUrl: stringBlocks[5]
                                ),
                            Livestream: new(
                                MedialViewCount: 0,
                                Popularity: 0,
                                HighestViewCount: 0,
                                HighestViewdUrl: ""
                                ),
                            Video: new(
                                MedialViewCount: 0,
                                Popularity: 0,
                                HighestViewCount: 0,
                                HighestViewdUrl: ""
                                )
                            )
                        );

                    TwitchStatistics twitchStatistics = new() {
                        RecentMedianViewCount = ulong.Parse(stringBlocks[7]),
                        RecentHighestViewCount = ulong.Parse(stringBlocks[8]),
                        HighestViewedVideoURL = stringBlocks[9],
                    };
                    twitchStatistics.UpdateFollowerCount(ulong.Parse(stringBlocks[6]));

                    return (youTubeRecord, twitchStatistics);
                }
            case Version.V4: {
                    // V4
                    // ID,
                    // YouTube Subscriber Count,YouTube View Count,YouTube Recent Median View Count,YouTube Recent Popularity,YouTube Recent Highest View Count,YouTube Recent Highest Viewed Video URL,
                    // Twitch Follower Count,Twitch Recent Median View Count,Twitch Recent Popularity,Twitch Recent Highest View Count,Twitch Recent Highest Viewed Video URL
                    YouTubeRecord youTubeRecord = new(
                        Basic: new(
                            SubscriberCount: ulong.Parse(stringBlocks[1]),
                            ViewCount: ulong.Parse(stringBlocks[2])
                            ),
                        Recent: new(
                            Total: new(
                                MedialViewCount: ulong.Parse(stringBlocks[3]),
                                Popularity: ulong.Parse(stringBlocks[4]),
                                HighestViewCount: ulong.Parse(stringBlocks[5]),
                                HighestViewdUrl: stringBlocks[6]
                                ),
                            Livestream: new(
                                MedialViewCount: 0,
                                Popularity: 0,
                                HighestViewCount: 0,
                                HighestViewdUrl: ""
                                ),
                            Video: new(
                                MedialViewCount: 0,
                                Popularity: 0,
                                HighestViewCount: 0,
                                HighestViewdUrl: ""
                                )
                            )
                        );

                    TwitchStatistics twitchStatistics = new() {
                        RecentMedianViewCount = ulong.Parse(stringBlocks[8]),
                        RecentPopularity = ulong.Parse(stringBlocks[9]),
                        RecentHighestViewCount = ulong.Parse(stringBlocks[10]),
                        HighestViewedVideoURL = stringBlocks[11],
                    };
                    twitchStatistics.UpdateFollowerCount(ulong.Parse(stringBlocks[7]));

                    return (youTubeRecord, twitchStatistics);
                }
            case Version.V5: {
                    // (V5)VTuber ID,
                    // YouTube Subscriber Count,YouTube View Count,
                    // YouTube Recent Total Median View Count,YouTube Recent Total Popularity,YouTube Recent Total Highest View Count,YouTube Recent Total Highest Viewed URL,
                    // YouTube Recent Livestream Median View Count,YouTube Recent Livestream Popularity,YouTube Recent Livestream Highest View Count,YouTube Recent Livestream Highest Viewed URL,
                    // YouTube Recent Video Median View Count,YouTube Recent Video Popularity,YouTube Recent Video Highest View Count,YouTube Recent Video Highest Viewed URL,
                    // Twitch Follower Count,Twitch Recent Median View Count,Twitch Recent Popularity,Twitch Recent Highest View Count,Twitch Recent Highest Viewed URL
                    YouTubeRecord youTubeRecord = new(
                        Basic: new(
                            SubscriberCount: ulong.Parse(stringBlocks[1]),
                            ViewCount: ulong.Parse(stringBlocks[2])
                            ),
                        Recent: new(
                            Total: new(
                                MedialViewCount: ulong.Parse(stringBlocks[3]),
                                Popularity: ulong.Parse(stringBlocks[4]),
                                HighestViewCount: ulong.Parse(stringBlocks[5]),
                                HighestViewdUrl: stringBlocks[6]
                                ),
                            Livestream: new(
                                MedialViewCount: ulong.Parse(stringBlocks[7]),
                                Popularity: ulong.Parse(stringBlocks[8]),
                                HighestViewCount: ulong.Parse(stringBlocks[9]),
                                HighestViewdUrl: stringBlocks[10]
                                ),
                            Video: new(
                                MedialViewCount: ulong.Parse(stringBlocks[11]),
                                Popularity: ulong.Parse(stringBlocks[12]),
                                HighestViewCount: ulong.Parse(stringBlocks[13]),
                                HighestViewdUrl: stringBlocks[14]
                                )
                            )
                        );

                    TwitchStatistics twitchStatistics = new() {
                        RecentMedianViewCount = ulong.Parse(stringBlocks[16]),
                        RecentPopularity = ulong.Parse(stringBlocks[17]),
                        RecentHighestViewCount = ulong.Parse(stringBlocks[18]),
                        HighestViewedVideoURL = stringBlocks[19],
                    };
                    twitchStatistics.UpdateFollowerCount(ulong.Parse(stringBlocks[15]));

                    return (youTubeRecord, twitchStatistics);
                }
            case Version.Unknown:
            default:
                throw new Exception("Unknown CSV version.");
        }
    }

    private static YouTubeStatistics CombineYouTubeRecord(YouTubeStatistics statistics, YouTubeRecord record) {
        YouTubeRecord combinedYouTubeRecord = new(
                Basic: new(
                    SubscriberCount: statistics.Basic.SubscriberCount + record.Basic.SubscriberCount,
                    ViewCount: statistics.Basic.ViewCount + record.Basic.ViewCount
                    ),
                Recent: new(
                    Total: CombineRecentRecord(statistics.Recent.Total, record.Recent.Total),
                    Livestream: CombineRecentRecord(statistics.Recent.Livestream, record.Recent.Livestream),
                    Video: CombineRecentRecord(statistics.Recent.Video, record.Recent.Video)
                    )
                );

        return new YouTubeStatistics(combinedYouTubeRecord);
    }

    private static YouTubeStatistics CombineYouTubeStatistics(decimal preRatio, YouTubeStatistics preStat, YouTubeStatistics postStat) {
        decimal postRatio = 1m - preRatio;

        YouTubeRecord combinedYouTubeRecord = new(
                Basic: new(
                    SubscriberCount: (ulong)(preRatio * preStat.Basic.SubscriberCount + postRatio * postStat.Basic.SubscriberCount),
                    ViewCount: (ulong)(preRatio * preStat.Basic.ViewCount + postRatio * postStat.Basic.ViewCount)
                    ),
                Recent: new(
                    Total: CombineRecentRecord(preRatio, preStat.Recent.Total, postStat.Recent.Total),
                    Livestream: CombineRecentRecord(preRatio, preStat.Recent.Livestream, postStat.Recent.Livestream),
                    Video: CombineRecentRecord(preRatio, preStat.Recent.Video, postStat.Recent.Video)
                    )
                );

        return new YouTubeStatistics(combinedYouTubeRecord);
    }

    private static YouTubeRecord.RecentRecord CombineRecentRecord(
        YouTubeRecord.RecentRecord a, YouTubeRecord.RecentRecord b) {
        return new(
            MedialViewCount: a.MedialViewCount + b.MedialViewCount,
            Popularity: a.Popularity + b.Popularity,
            HighestViewCount: Math.Max(a.HighestViewCount, b.HighestViewCount),
            HighestViewdUrl: a.HighestViewCount > b.HighestViewCount ? a.HighestViewdUrl : b.HighestViewdUrl
            );
    }

    private static YouTubeRecord.RecentRecord CombineRecentRecord(
    decimal preRatio, YouTubeRecord.RecentRecord a, YouTubeRecord.RecentRecord b) {
        decimal postRatio = 1m - preRatio;

        return new(
            MedialViewCount: (ulong)(preRatio * a.MedialViewCount + postRatio * b.MedialViewCount),
            Popularity: (ulong)(preRatio * a.MedialViewCount + postRatio * b.MedialViewCount),
            HighestViewCount: (ulong)(preRatio * a.HighestViewCount + postRatio * b.HighestViewCount),
            HighestViewdUrl: a.HighestViewCount > b.HighestViewCount ? a.HighestViewdUrl : b.HighestViewdUrl
            );
    }

    private static TwitchStatistics CombineTwitchStatistics(TwitchStatistics a, TwitchStatistics b) {
        a.RecentMedianViewCount += b.RecentMedianViewCount;
        a.RecentPopularity += b.RecentPopularity;
        a.UpdateFollowerCount(a.FollowerCount + b.FollowerCount);

        if (b.RecentHighestViewCount > a.RecentHighestViewCount) {
            a.RecentHighestViewCount = b.RecentHighestViewCount;
            a.HighestViewedVideoURL = b.HighestViewedVideoURL;
        }

        return a;
    }

    private void UpdateCombinedValue() {
        this.CombinedRecentTotalStatistics = new(
            MedianViewCount: this.YouTube.Recent.Total.MedialViewCount + this.Twitch.RecentMedianViewCount,
            Popularity: this.YouTube.Recent.Total.Popularity + this.Twitch.RecentPopularity
            );

        this.CombinedRecentLivestreamStatistics = new(
            MedianViewCount: this.YouTube.Recent.Livestream.MedialViewCount + this.Twitch.RecentMedianViewCount,
            Popularity: this.YouTube.Recent.Livestream.Popularity + this.Twitch.RecentPopularity
            );

        this.CombinedRecentVideoStatistics = new(
            MedianViewCount: this.YouTube.Recent.Video.MedialViewCount,
            Popularity: this.YouTube.Recent.Video.Popularity
            );
    }
}