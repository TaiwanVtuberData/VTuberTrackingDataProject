namespace GenerateJsonFile.Types;

internal record GroupData(
    string id,
    string name,
    ulong popularity,
    ulong livestreamPopularity,
    ulong videoPopularity,
    List<VTuberData> members
    );

internal record GroupDataResponse(
    List<GroupData> groups);
