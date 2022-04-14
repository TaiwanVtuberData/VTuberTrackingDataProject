namespace Common.Utils;
public class Utility
{
    public static string YouTubeVideoUrlToId(string Url)
    {
        return Url.Replace("https://www.youtube.com/watch?v=", "");
    }

    public static string TwitchVideoUrlToId(string Url)
    {
        return Url.Replace("https://www.twitch.tv/videos/", "");
    }
}
