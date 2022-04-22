namespace Common.Types;
public readonly record struct VTuberData(
    string Id,
    string DisplayName,
    List<string> LstAliasName,
    string YouTubeChannelId,
    string TwitchChannelId,
    string TwitchChannelName,
    DateTime DebuteDate,
    DateTime GraduationDate,
    bool IsActive,
    string GroupName,
    string Nationality);
