namespace GenerateRecordList.Types;

public record GroupData(
    string id,
    string name,
    ulong popularity,
    ulong livestreamPopularity,
    ulong videoPopularity,
    List<VTuberData> members
    );

public record GroupDataResponse(
    List<GroupData> groups);
