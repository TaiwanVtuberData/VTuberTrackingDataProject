using Common.Utils;

namespace FetchStatistics;
internal record Config {
    public string youTubeApiKey { get; init; }
    public FetchTwitchStatistics.Fetcher.Credential twitchCrenditial { get; init; }
    public string trackListPath { get; init; }
    public string excludeListPath { get; init; }
    public string savePath { get; init; }

    public Config(string[] filePaths, string[] defaultArgs) {
        // set file paths
        string youTubeApiKeyPath = filePaths.Length >= 1 ? filePaths[0] : defaultArgs[0];
        string twitchClientIdPath = filePaths.Length >= 2 ? filePaths[1] : defaultArgs[1];
        string twitchSecretPath = filePaths.Length >= 3 ? filePaths[2] : defaultArgs[2];
        string trackListPath = filePaths.Length >= 4 ? filePaths[3] : defaultArgs[3];
        string excludeListPath = filePaths.Length >= 5 ? filePaths[4] : defaultArgs[4];
        string savePath = filePaths.Length >= 6 ? filePaths[5] : defaultArgs[5];

        // initialize fields
        this.youTubeApiKey = FileUtility.GetSingleLineFromFile(youTubeApiKeyPath);
        this.twitchCrenditial = new(FileUtility.GetSingleLineFromFile(twitchClientIdPath),
        FileUtility.GetSingleLineFromFile(twitchSecretPath));
        this.trackListPath = trackListPath;
        this.excludeListPath = excludeListPath;
        this.savePath = savePath;
    }
}
