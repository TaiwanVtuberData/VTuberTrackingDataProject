using Common.Types;
using Common.Types.Basic;
using GenerateJsonFile.Types;
using GenerateRecordList.Types;
using GenerateRecordList.Utils;

namespace GenerateJsonFile;

internal class LiveVideosListToJsonStruct(string nationalityFilter, DateTimeOffset currentTime)
{
    public readonly string NationalityFilter = nationalityFilter;
    public readonly DateTimeOffset CurrentTime = currentTime;

    public List<LivestreamData> Get(
        LiveVideosList liveVideosList,
        List<DebutData> lstDebutData,
        DictionaryRecord dictRecord,
        bool noTitle
    )
    {
        List<LivestreamData> rLst = [];

        List<VTuberId> lstValidVTubers = dictRecord.Keys.ToList();

        foreach (
            LiveVideoInformation videoInfo in liveVideosList
                .Where(p => lstValidVTubers.Contains(p.Id))
                .Where(p => dictRecord[p.Id].Nationality.Contains(NationalityFilter))
                .Where(p => IsActuallyLiveOrUpcoming(p, CurrentTime))
                .OrderBy(p => p.PublishDateTime)
        )
        {
            VTuberRecord record = dictRecord[videoInfo.Id];

            LivestreamData livestreamsData =
                new(
                    id: record.Id,
                    name: record.DisplayName,
                    imgUrl: record.ImageUrl,
                    title: noTitle ? null : videoInfo.Title,
                    videoUrl: videoInfo.Url,
                    thumbnailUrl: MiscUtils.SetTwitchLivestreamThumbnailUrlSize(
                        videoInfo.ThumbnailUrl,
                        width: 178,
                        height: 100
                    ),
                    startTime: videoInfo.PublishDateTime != DateTime.UnixEpoch
                        ? MiscUtils.ToIso8601UtcString(videoInfo.PublishDateTime)
                        : null
                );

            rLst.Add(livestreamsData);
        }

        foreach (DebutData debutData in lstDebutData)
        {
            if (rLst.Where(e => e.videoUrl == debutData.VideoUrl).Any())
            {
                continue;
            }

            if (!dictRecord.ContainsKey(debutData.Id))
            {
                continue;
            }

            VTuberRecord record = dictRecord[debutData.Id];

            if (record.Nationality.Contains(NationalityFilter))
            {
                LivestreamData livestreamsData =
                    new(
                        id: record.Id,
                        name: record.DisplayName,
                        imgUrl: record.ImageUrl,
                        title: null,
                        videoUrl: debutData.VideoUrl,
                        thumbnailUrl: debutData.ThumbnailUrl,
                        startTime: MiscUtils.ToIso8601UtcString(debutData.StartTime)
                    );

                rLst.Add(livestreamsData);
            }
        }

        return rLst;
    }

    public List<LivestreamData> GetDebutToday(
        LiveVideosList liveVideosList,
        List<DebutData> lstDebutData,
        DictionaryRecord dictRecord,
        bool noTitle
    )
    {
        List<LivestreamData> rLst = [];

        List<VTuberId> lstDebutVTubers = dictRecord
            .GetAboutToDebutList(DateOnly.FromDateTime(CurrentTime.DateTime))
            .Select(e => e.Id)
            .ToList();

        foreach (
            LiveVideoInformation videoInfo in liveVideosList
                .Where(p => lstDebutVTubers.Contains(p.Id))
                .Where(p => dictRecord[p.Id].Nationality.Contains(NationalityFilter))
                .Where(p => IsActuallyLiveOrUpcoming(p, CurrentTime))
                .OrderBy(p => p.PublishDateTime)
        )
        {
            VTuberRecord record = dictRecord[videoInfo.Id];

            LivestreamData livestreamsData =
                new(
                    id: record.Id,
                    name: record.DisplayName,
                    imgUrl: record.ImageUrl,
                    title: noTitle ? null : videoInfo.Title,
                    videoUrl: videoInfo.Url,
                    thumbnailUrl: MiscUtils.SetTwitchLivestreamThumbnailUrlSize(
                        videoInfo.ThumbnailUrl,
                        width: 178,
                        height: 100
                    ),
                    startTime: videoInfo.PublishDateTime != DateTime.UnixEpoch
                        ? MiscUtils.ToIso8601UtcString(videoInfo.PublishDateTime)
                        : null
                );

            rLst.Add(livestreamsData);
        }

        foreach (DebutData debutData in lstDebutData)
        {
            if (rLst.Where(e => e.videoUrl == debutData.VideoUrl).Any())
            {
                continue;
            }

            if (!dictRecord.ContainsKey(debutData.Id))
            {
                continue;
            }

            VTuberRecord record = dictRecord[debutData.Id];

            if (record.Nationality.Contains(NationalityFilter))
            {
                LivestreamData livestreamsData =
                    new(
                        id: record.Id,
                        name: record.DisplayName,
                        imgUrl: record.ImageUrl,
                        title: null,
                        videoUrl: debutData.VideoUrl,
                        thumbnailUrl: debutData.ThumbnailUrl,
                        startTime: MiscUtils.ToIso8601UtcString(debutData.StartTime)
                    );

                rLst.Add(livestreamsData);
            }
        }

        return rLst;
    }

    private static bool IsActuallyLiveOrUpcoming(
        LiveVideoInformation videoInfo,
        DateTimeOffset currentTime
    )
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
