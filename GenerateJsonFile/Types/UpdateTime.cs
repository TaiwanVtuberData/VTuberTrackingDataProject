namespace GenerateJsonFile.Types;
internal record UpdateTime(
    string statisticUpdateTime,
    string VTuberDataUpdateTime);

internal record UpdateTimeResponse(
    UpdateTime time);
