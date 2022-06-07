namespace GenerateJsonFile.Utils;
internal class MiscUtils
{
    public static string SetTwitchThumbnailUrlSize(string str, int width, int height)
    {
        if (!str.Contains("%{width}") || !str.Contains("%{height}"))
            return str;

        return str
            .Replace("%{width}", width.ToString())
            .Replace("%{height}", height.ToString());
    }

    public static string SetTwitchLivestreamThumbnailUrlSize(string str, int width, int height)
    {
        if (!str.Contains("{width}") || !str.Contains("{height}"))
            return str;

        return str
            .Replace("{width}", width.ToString())
            .Replace("{height}", height.ToString());
    }

    public static string ToIso8601UtcString(DateTime dateTime)
    {
        return dateTime.ToUniversalTime().ToString("o");
    }
}
