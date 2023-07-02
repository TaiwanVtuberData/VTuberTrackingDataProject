namespace GenerateJsonFile.Types;

internal record GroupData(
    string id,
    string name,
    ulong popularity,
    ulong liveStreamPopularity,
    ulong videoPopularity,
    List<VTuberData> members
    );

internal record GroupDataResponse(
    List<GroupData> groups);
