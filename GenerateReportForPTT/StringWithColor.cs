namespace GenerateReportForPTT;

readonly record struct StringWithColor(string Str, ColorCode Color)
{
    public override string ToString()
    {
        return this.Str;
    }
}
