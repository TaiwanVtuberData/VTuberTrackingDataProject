namespace Common.Types;

public enum LiveVideoType
{
    live,
    upcoming,
}

public class LiveVideoTypeConvert
{
    public static bool IsLiveVideoType(string input)
    {
        return Enum.TryParse(value: input, out LiveVideoType _);
    }

    public static LiveVideoType FromString(string input)
    {
        bool success = Enum.TryParse(value: input, out LiveVideoType result);

        if (success)
        {
            return result;
        }

        return LiveVideoType.upcoming;
    }
}