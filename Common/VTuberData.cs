﻿namespace Common;
public readonly record struct VTuberData(
    string DisplayName,
    List<string> LstAliasName,
    string YouTubeChannelId,
    string TwitchChannelId,
    string TwitchChannelName,
    DateTime DebuteDate,
    DateTime GraduationDate,
    bool IsActive,
    string GroupName,
    string Nationality,
    int ImportanceLevel);
