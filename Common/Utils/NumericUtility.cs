namespace Common.Utils;
public class NumericUtility
{
    private static int CompareTupleThirdValue(Tuple<DateTime, string, ulong> v1, Tuple<DateTime, string, ulong> v2)
    {
        return Comparer<ulong>.Default.Compare(v1.Item3, v2.Item3);
    }

    public static ulong GetMedian(List<Tuple<DateTime, string, ulong>> list)
    {
        List<Tuple<DateTime, string, ulong>> newList = new(list);

        newList.Sort(CompareTupleThirdValue);

        if (newList.Count == 0)
            return 0;

        if (newList.Count == 1)
            return list[0].Item3;

        if (newList.Count % 2 == 1)
            return newList[newList.Count / 2].Item3;
        else
            return (newList[newList.Count / 2 - 1].Item3 + newList[newList.Count / 2].Item3) / 2;
    }

    private static decimal GetMedian(List<decimal> list)
    {
        List<decimal> newList = new(list);

        newList.Sort();

        if (newList.Count == 0)
            return 0;

        if (newList.Count == 1)
            return list[0];

        if (newList.Count % 2 == 1)
            return newList[newList.Count / 2];
        else
            return (newList[newList.Count / 2 - 1] + newList[newList.Count / 2]) / 2;
    }

    public static Tuple<DateTime, string, ulong> GetLargest(List<Tuple<DateTime, string, ulong>> list)
    {
        return list.MaxBy(e => e.Item3) ?? new Tuple<DateTime, string, ulong>(DateTime.UnixEpoch, "", 0);
    }

    public static decimal GetPopularity(List<Tuple<DateTime, string, ulong>> list, DateTime currentTime)
    {
        if (list.Count <= 0)
            return 0m;

        List<decimal> newList = list.Select(e => e.Item3 * Get30DaysRatio(currentTime, e.Item1)).ToList();

        return GetMedian(newList);
    }

    // (decimal)TimeSpan.FromDays(30).TotalMilliseconds
    private const decimal _30DaysMilliseconds = 2592000000;

    private static decimal Get30DaysRatio(DateTime currentTime, DateTime targetTime)
    {
        decimal ratioMilliseconds = (decimal)(currentTime.ToUniversalTime() - targetTime.ToUniversalTime()).TotalMilliseconds;
        if (ratioMilliseconds < 0m)
            ratioMilliseconds = 0m;

        return 1m - (ratioMilliseconds / _30DaysMilliseconds);
    }
}
