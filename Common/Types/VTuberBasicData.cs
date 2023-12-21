using Common.Types.Basic;
using Microsoft.VisualBasic.FileIO;

namespace Common.Types;
public readonly record struct VTuberBasicData(VTuberId Id, YouTubeData? YouTube, TwitchData? Twitch) {
    public string GetRepresentImageUrl() {
        if (this.YouTube == null && this.Twitch == null) {
            throw new Exception("Malformed Basic Data CSV file.");
        }

        if (this.Twitch == null) {
            return this.YouTube?.ThumbnailUrl;
        }

        if (this.YouTube == null) {
            return this.Twitch?.ThumbnailUrl;
        }

        if (this.YouTube?.SubscriberCount > this.Twitch?.FollowerCount)
            return this.YouTube?.ThumbnailUrl;
        else
            return this.Twitch?.ThumbnailUrl;
    }

    private static readonly Dictionary<string, int> csvHeaderIndexs = new()
    {
        { "VTuber ID", 0 },
        { "YouTube Subscriber Count", 1 },
        { "YouTube View Count", 2 },
        { "YouTube Thumbnail URL", 3 },
        { "Twitch Follower Count", 4 },
        { "Twitch Thumbnail URL", 5 },
    };

    public static List<VTuberBasicData> ReadFromCsvAsList(string csvFilePath) {
        TextFieldParser reader = new(csvFilePath) {
            HasFieldsEnclosedInQuotes = true,
            Delimiters = [","],
            CommentTokens = ["#"],
            TrimWhiteSpace = false,
            TextFieldType = FieldType.Delimited,
        };

        // consume header
        string[]? headerBlock = reader.ReadFields();

        if (headerBlock is null || headerBlock.Length != csvHeaderIndexs.Count)
            return [];

        List<VTuberBasicData> rLst = [];

        while (!reader.EndOfData) {
            string[]? entryBlock = reader.ReadFields();

            if (entryBlock is null) {
                return [];
            }

            string id = entryBlock[csvHeaderIndexs["VTuber ID"]];
            bool hasYouTubeSubCount = ulong.TryParse(entryBlock[csvHeaderIndexs["YouTube Subscriber Count"]], out ulong youTubeSubCount);
            bool hasYouTubeViewCount = ulong.TryParse(entryBlock[csvHeaderIndexs["YouTube View Count"]], out ulong youTubeViewCount);
            string youTubeImgUrl = entryBlock[csvHeaderIndexs["YouTube Thumbnail URL"]];
            bool hasTwitchFollowerCount = ulong.TryParse(entryBlock[csvHeaderIndexs["Twitch Follower Count"]], out ulong twitchFollowerCount);
            string twitchImgUrl = entryBlock[csvHeaderIndexs["Twitch Thumbnail URL"]];

            bool hasYouTube = youTubeImgUrl != "";
            bool hasTwitch = twitchImgUrl != "";

            YouTubeData? youTubeData = hasYouTube ?
                new YouTubeData(SubscriberCount: hasYouTubeSubCount ? youTubeSubCount : null,
                ViewCount: hasYouTubeViewCount ? youTubeViewCount : null,
                ThumbnailUrl: youTubeImgUrl)
                : null;

            TwitchData? twitchData = hasTwitch ?
                new TwitchData(FollowerCount: twitchFollowerCount,
                ThumbnailUrl: twitchImgUrl)
                : null;

            rLst.Add(new VTuberBasicData() { Id = new VTuberId(id), YouTube = youTubeData, Twitch = twitchData });
        }

        return rLst;
    }

    public static Dictionary<VTuberId, VTuberBasicData> ReadFromCsv(string csvFilePath) {
        List<VTuberBasicData> lstBasicData = ReadFromCsvAsList(csvFilePath);

        return lstBasicData.ToDictionary(
            t => t.Id,
            t => t);
    }

    public static void WriteToCsv(StreamWriter writer, List<VTuberBasicData> lstBasicData) {
        writer.WriteLine(string.Join(',', csvHeaderIndexs.Select(p => p.Key)));

        foreach (VTuberBasicData data in lstBasicData.OrderBy(p => p.Id)) {
            writer.WriteLine($"{data.Id.Value}," +
                $"{(data.YouTube.HasValue ? data.YouTube.Value.SubscriberCount : "")}," +
                $"{(data.YouTube.HasValue ? data.YouTube.Value.ViewCount : "")}," +
                $"{(data.YouTube.HasValue ? data.YouTube.Value.ThumbnailUrl : "")}," +
                $"{(data.Twitch.HasValue ? data.Twitch.Value.FollowerCount : "")}," +
                $"{(data.Twitch.HasValue ? data.Twitch.Value.ThumbnailUrl : "")}");
        }
    }
}
