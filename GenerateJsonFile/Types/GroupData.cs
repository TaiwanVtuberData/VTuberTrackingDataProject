namespace GenerateJsonFile.Types;

internal readonly record struct GroupData(
    string id,
    string name,
    ulong popularity,
    List<VTuberData> members);

internal readonly record struct GroupDataResponse(
    List<GroupData> groups);
