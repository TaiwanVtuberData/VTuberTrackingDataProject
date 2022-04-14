using Microsoft.VisualBasic.FileIO;

namespace Common.Types;
public class VTuberBasicData
{
    public string DisplayName { get; init; }
    public YouTubeData? YouTube { get; init; }
    public TwitchData? Twitch { get; init; }

    public string GetRepresentImageUrl()
    {
        if (this.YouTube == null && this.Twitch == null)
        {
            throw new System.Exception("Malformed Basic Data CSV file.");
        }

        if (this.Twitch == null)
        {
            return this.YouTube?.ThumbnailUrl;
        }

        if (this.YouTube == null)
        {
            return this.Twitch?.ThumbnailUrl;
        }

        if (this.YouTube?.SubscriberCount > this.Twitch?.FollowerCount)
            return this.YouTube?.ThumbnailUrl;
        else
            return this.Twitch?.ThumbnailUrl;
    }

    private static readonly Dictionary<string, int> csvHeaderIndexs = new()
    {
        { "Display Name", 0 },
        { "YouTube Subscriber Count", 1 },
        { "YouTube View Count", 2 },
        { "YouTube Thumbnail URL", 3 },
        { "Twitch Follower Count", 4 },
        { "Twitch Thumbnail URL", 5 },
    };

    public static Dictionary<string, VTuberBasicData> ReadFromCsv(string csvFilePath)
    {
        TextFieldParser reader = new(csvFilePath)
        {
            HasFieldsEnclosedInQuotes = true,
            Delimiters = new string[] { "," },
            CommentTokens = new string[] { "#" },
            TrimWhiteSpace = false,
            TextFieldType = FieldType.Delimited,
        };

        // consume header
        string[]? headerBlock = reader.ReadFields();

        if (headerBlock is null || headerBlock.Length != csvHeaderIndexs.Count)
            return new();

        Dictionary<string, VTuberBasicData> rDict = new();

        while (!reader.EndOfData)
        {
            string[]? entryBlock = reader.ReadFields();

            if (entryBlock is null)
            {
                return new();
            }

            string displayName = entryBlock[csvHeaderIndexs["Display Name"]];
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

            rDict.Add(displayName, new VTuberBasicData() { DisplayName = displayName, YouTube = youTubeData, Twitch = twitchData });
        }

        return rDict;
    }
}
