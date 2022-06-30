namespace Common.Utils;
public class NumericUtility
{
    private static int CompareTupleSecondValue(Tuple<string, ulong> v1, Tuple<string, ulong> v2)
    {
        return Comparer<ulong>.Default.Compare(v1.Item2, v2.Item2);
    }

    public static ulong GetMedian(List<Tuple<string, ulong>> list)
    {
        list.Sort(CompareTupleSecondValue);

        if (list.Count == 0)
            return 0;

        if (list.Count == 1)
            return list[0].Item2;

        if (list.Count % 2 == 1)
            return list[list.Count / 2].Item2;
        else
            return (list[list.Count / 2 - 1].Item2 + list[list.Count / 2].Item2) / 2;
    }

    public static Tuple<string, ulong> GetLargest(List<Tuple<string, ulong>> list)
    {
        return list.MaxBy(e => e.Item2) ?? new Tuple<string, ulong>("", 0);
    }
}
