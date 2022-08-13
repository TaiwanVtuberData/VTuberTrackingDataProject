namespace GenerateJsonFile.Types;
internal readonly record struct UpdateTime(
    string statisticUpdateTime,
    string VTuberDataUpdateTime);

internal readonly record struct UpdateTimeResponse(
    UpdateTime time);
