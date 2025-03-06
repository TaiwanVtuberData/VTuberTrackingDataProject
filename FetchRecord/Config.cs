using Common.Utils;
using log4net;

namespace FetchRecord;

internal record Config
{
    readonly ILog log = LogManager.GetLogger(typeof(Config));

    public string YouTubeApiKey { get; init; }
    public FetchTwitchStatistics.Fetcher.Credential TwitchCredential { get; init; }
    public string TrackListPath { get; init; }
    public string ExcludeListPath { get; init; }
    public string SavePath { get; init; }
    public FetchType FetchType { get; init; }
    public IgnoreType IgnoreType { get; init; }

    public Config()
    {
        string? YOUTUBE_API_KEY_PATH = Environment.GetEnvironmentVariable("YOUTUBE_API_KEY_PATH");
        log.Info($"YOUTUBE_API_KEY_PATH: {YOUTUBE_API_KEY_PATH}");

        string? TWITCH_CLIENT_ID_PATH = Environment.GetEnvironmentVariable("TWITCH_CLIENT_ID_PATH");
        log.Info($"TWITCH_CLIENT_ID_PATH: {TWITCH_CLIENT_ID_PATH}");

        string? TWITCH_SECRET_PATH = Environment.GetEnvironmentVariable("TWITCH_SECRET_PATH");
        log.Info($"TWITCH_SECRET_PATH: {TWITCH_SECRET_PATH}");

        string? TRACK_LIST_PATH = Environment.GetEnvironmentVariable("TRACK_LIST_PATH");
        log.Info($"TRACK_LIST_PATH: {TRACK_LIST_PATH}");

        string? EXCLUDE_LIST_PATH = Environment.GetEnvironmentVariable("EXCLUDE_LIST_PATH");
        log.Info($"EXCLUDE_LIST_PATH: {EXCLUDE_LIST_PATH}");

        string? SAVE_PATH = Environment.GetEnvironmentVariable("SAVE_PATH");
        log.Info($"SAVE_PATH: {SAVE_PATH}");

        string? FETCH_TYPE = Environment.GetEnvironmentVariable("FETCH_TYPE");
        log.Info($"FETCH_TYPE: {FETCH_TYPE}");

        string? IGNORE_TYPE = Environment.GetEnvironmentVariable("IGNORE_TYPE");
        log.Info($"IGNORE_TYPE: {IGNORE_TYPE}");

        if (
            YOUTUBE_API_KEY_PATH == null
            || TWITCH_CLIENT_ID_PATH == null
            || TWITCH_SECRET_PATH == null
            || TRACK_LIST_PATH == null
            || EXCLUDE_LIST_PATH == null
            || SAVE_PATH == null
            || FETCH_TYPE == null
            || IGNORE_TYPE == null
        )
        {
            log.Error("Some environment variables are not set in Config.");
            throw new Exception("Some environment variables are not set in Config.");
        }

        // initialize fields
        this.YouTubeApiKey = FileUtility.GetSingleLineFromFile(YOUTUBE_API_KEY_PATH);
        this.TwitchCredential = new(
            FileUtility.GetSingleLineFromFile(TWITCH_CLIENT_ID_PATH),
            FileUtility.GetSingleLineFromFile(TWITCH_SECRET_PATH)
        );
        this.TrackListPath = TRACK_LIST_PATH;
        this.ExcludeListPath = EXCLUDE_LIST_PATH;
        this.SavePath = SAVE_PATH;
        this.FetchType = (FetchType)Enum.Parse(typeof(FetchType), FETCH_TYPE);
        this.IgnoreType = (IgnoreType)Enum.Parse(typeof(IgnoreType), IGNORE_TYPE);
    }
}
