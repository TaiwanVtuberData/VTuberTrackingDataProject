using Common.Utils;

namespace FetchRecord;
internal record Config {

    public string YouTubeApiKey { get; init; }
    public FetchTwitchStatistics.Fetcher.Credential TwitchCrenditial { get; init; }
    public string TrackListPath { get; init; }
    public string ExcludeListPath { get; init; }
    public string SavePath { get; init; }
    public FetchType FetchType { get; init; }

    public Config(string[] filePaths, string[] defaultArgs) {
        // set file paths
        string youTubeApiKeyPath = filePaths.Length >= 1 ? filePaths[0] : defaultArgs[0];
        string twitchClientIdPath = filePaths.Length >= 2 ? filePaths[1] : defaultArgs[1];
        string twitchSecretPath = filePaths.Length >= 3 ? filePaths[2] : defaultArgs[2];
        string trackListPath = filePaths.Length >= 4 ? filePaths[3] : defaultArgs[3];
        string excludeListPath = filePaths.Length >= 5 ? filePaths[4] : defaultArgs[4];
        string savePath = filePaths.Length >= 6 ? filePaths[5] : defaultArgs[5];
        string fetchTypeStr = filePaths.Length >= 7 ? filePaths[6] : defaultArgs[6];

        // initialize fields
        this.YouTubeApiKey = FileUtility.GetSingleLineFromFile(youTubeApiKeyPath);
        this.TwitchCrenditial = new(FileUtility.GetSingleLineFromFile(twitchClientIdPath),
        FileUtility.GetSingleLineFromFile(twitchSecretPath));
        this.TrackListPath = trackListPath;
        this.ExcludeListPath = excludeListPath;
        this.SavePath = savePath;
        this.FetchType = (FetchType)Enum.Parse(typeof(FetchType), fetchTypeStr);
    }
}
