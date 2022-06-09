using Common.Types;
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

    public List<LivestreamData> Get(LiveVideosList liveVideosList, List<DebutData> lstDebutData, DictionaryRecord dictRecord, bool noTitle)
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
                startTime: videoInfo.PublishDateTime != DateTime.UnixEpoch ? MiscUtils.ToIso8601UtcString(videoInfo.PublishDateTime) : null
            );

            rLst.Add(livestreamsData);
        }

        foreach (DebutData debutData in lstDebutData)
        {
            VTuberRecord record = dictRecord.GetRecordByYouTubeId(debutData.YouTubeId);

            if (record.Nationality.Contains(NationalityFilter))
            {
                LivestreamData livestreamsData = new(
                    id: record.Id,
                    name: record.DisplayName,
                    imgUrl: record.ImageUrl,
                    title: null,
                    videoUrl: $"https://www.youtube.com/watch?v={debutData.VideoId}",
                    thumbnailUrl: null,
                    startTime: MiscUtils.ToIso8601UtcString(debutData.StartTime)
                    );

                rLst.Add(livestreamsData);
            }
        }

        return rLst;
    }

    public List<LivestreamData> GetDebutToday(LiveVideosList liveVideosList, List<DebutData> lstDebutData, DictionaryRecord dictRecord, bool noTitle)
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
                startTime: videoInfo.PublishDateTime != DateTime.UnixEpoch ? MiscUtils.ToIso8601UtcString(videoInfo.PublishDateTime) : null
            );

            rLst.Add(livestreamsData);
        }

        foreach (DebutData debutData in lstDebutData)
        {
            VTuberRecord record = dictRecord.GetRecordByYouTubeId(debutData.YouTubeId);

            if (record.Nationality.Contains(NationalityFilter))
            {
                LivestreamData livestreamsData = new(
                    id: record.Id,
                    name: record.DisplayName,
                    imgUrl: record.ImageUrl,
                    title: null,
                    videoUrl: $"https://www.youtube.com/watch?v={debutData.VideoId}",
                    thumbnailUrl: null,
                    startTime: MiscUtils.ToIso8601UtcString(debutData.StartTime)
                    );

                rLst.Add(livestreamsData);
            }
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
