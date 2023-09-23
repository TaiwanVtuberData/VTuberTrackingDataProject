using Common.Types.Basic;

namespace Common.Types;
public readonly record struct VTuberData(
    VTuberId Id,
    string DisplayName,
    List<string> LstAliasName,
    string YouTubeChannelId,
    string TwitchChannelId,
    string TwitchChannelName,
    DateOnly? DebuteDate,
    DateOnly? GraduationDate,
    Activity Activity,
    string? GroupName,
    string Nationality);
