namespace GenerateJsonFile.Types;

class YouTubeGrowthData : YouTubeData
{
    public GrowthData _7DaysGrowth { get; set; } = new();
    public GrowthData _30DaysGrowth { get; set; } = new();
    public string Nationality { get; set; } = "";
}
