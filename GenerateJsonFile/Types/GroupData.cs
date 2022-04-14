namespace GenerateJsonFile.Types;

class GroupData
{
    public string id { get; set; } = "";
    public string name { get; set; } = "";
    public ulong popularity { get; set; } = 0;
    public List<VTuberData> members { get; set; } = new();
}

