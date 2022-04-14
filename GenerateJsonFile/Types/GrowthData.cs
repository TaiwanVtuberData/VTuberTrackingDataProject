namespace GenerateJsonFile.Types;

class GrowthData
{
    public decimal diff { get; set; } = 0m;
    // recordType: 'none' | 'partial' | 'full';
    public string recordType { get; set; } = "none";
}
