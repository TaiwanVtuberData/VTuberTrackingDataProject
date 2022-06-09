﻿using Common.Types;
using GenerateJsonFile.Types;
using GenerateJsonFile.Utils;

namespace GenerateJsonFile;
internal class LiveVideosListToJsonStruct
{
    public readonly string NationalityFilter;
    public readonly DateTime CurrentTime;

    public LiveVideosListToJsonStruct(string nationalityFilter, DateTime currentTime)
    {
        NationalityFilter = nationalityFilter;
        CurrentTime = currentTime;
    }

    public List<LivestreamData> Get(LiveVideosList liveVideosList, DictionaryRecord dictRecord, bool noTitle)
    {
        List<LivestreamData> rLst = new();

        List<string> lstValidVTubers = dictRecord.Keys.ToList();

        foreach (LiveVideoInformation videoInfo in liveVideosList
            .Where(p => lstValidVTubers.Contains(p.Id))
            .Where(p => dictRecord[p.Id].Nationality.Contains(NationalityFilter))
            .Where(p => IsActuallyLiveOrUpcoming(p, CurrentTime)))
        {
            VTuberRecord record = dictRecord[videoInfo.Id];

            LivestreamData livestreamsData = new(
                id: record.Id,
                name: record.DisplayName,
                imgUrl: record.ImageUrl,
                title: noTitle ? null : videoInfo.Title,
                videoUrl: videoInfo.Url,
                thumbnailUrl: MiscUtils.SetTwitchLivestreamThumbnailUrlSize(videoInfo.ThumbnailUrl, width: 178, height: 100),
                // https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-date-and-time-format-strings#Roundtrip
                // use "o" specifier to get correct format 2009-06-15T13:45:30.0000000Z
                startTime: videoInfo.PublishDateTime != DateTime.UnixEpoch ? MiscUtils.ToIso8601UtcString(videoInfo.PublishDateTime) : null
            );
            ;

            rLst.Add(livestreamsData);
        }

        return rLst;
    }

    public List<LivestreamData> GetDebutToday(LiveVideosList liveVideosList, DictionaryRecord dictRecord, bool noTitle)
    {
        List<LivestreamData> rLst = new();

        List<string> lstDebutVTubers = dictRecord.GetAboutToDebutList(DateOnly.FromDateTime(CurrentTime.ToLocalTime())).Select(x => x.Id).ToList();

        foreach (LiveVideoInformation videoInfo in liveVideosList
            .Where(p => lstDebutVTubers.Contains(p.Id))
            .Where(p => dictRecord[p.Id].Nationality.Contains(NationalityFilter))
            .Where(p => IsActuallyLiveOrUpcoming(p, CurrentTime)))
        {
            VTuberRecord record = dictRecord[videoInfo.Id];

            LivestreamData livestreamsData = new(
                id: record.Id,
                name: record.DisplayName,
                imgUrl: record.ImageUrl,
                title: noTitle ? null : videoInfo.Title,
                videoUrl: videoInfo.Url,
                thumbnailUrl: MiscUtils.SetTwitchLivestreamThumbnailUrlSize(videoInfo.ThumbnailUrl, width: 178, height: 100),
                // https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-date-and-time-format-strings#Roundtrip
                // use "o" specifier to get correct format 2009-06-15T13:45:30.0000000Z
                startTime: videoInfo.PublishDateTime != DateTime.UnixEpoch ? MiscUtils.ToIso8601UtcString(videoInfo.PublishDateTime) : null
            );
            ;

            rLst.Add(livestreamsData);
        }

        return rLst;
    }

    private static bool IsActuallyLiveOrUpcoming(LiveVideoInformation videoInfo, DateTime currentTime)
    {
        switch (videoInfo.VideoType)
        {
            case LiveVideoType.live:
                return true;
            case LiveVideoType.upcoming:
                {
                    if (videoInfo.PublishDateTime < currentTime.AddDays(-1))
                    {
                        return false;
                    }

                    if (videoInfo.PublishDateTime > currentTime.AddDays(1))
                    {
                        return false;
                    }

                    return true;
                }
            default:
                return false;
        }
    }
}
