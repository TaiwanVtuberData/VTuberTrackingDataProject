using Common.Types;
using Common.Types.Basic;
using GenerateJsonFile.Types;
using GenerateRecordList.Types;
using GenerateRecordList.Utils;
using LanguageExt;

namespace GenerateJsonFile;

public class TopVideosListToJsonStruct(string nationalityFilter)
{
    public readonly string NationalityFilter = nationalityFilter;

    public enum FilterOption
    {
        Before,
        AfterOrEqual,
    }

    public record VTuberFilterOption(FilterOption FilterOption, DateOnly DebutDate) { }

    class OwnerComparer : IEqualityComparer<VideoInformation>
    {
        public bool Equals(VideoInformation? x, VideoInformation? y)
        {
            if (x is null && y is null)
                return false;

            if (x is null)
                return false;

            if (y is null)
                return false;

            return x.Id == y.Id;
        }

        int IEqualityComparer<VideoInformation>.GetHashCode(VideoInformation obj)
        {
            return obj.GetHashCode();
        }
    }

    public List<VideoPopularityData> Get(
        TopVideosList topVideoList,
        DictionaryRecord dictRecord,
        int count,
        bool allowDuplicate,
        VTuberFilterOption? vTuberFilterOption
    )
    {
        List<VideoPopularityData> rLst = [];

        List<VideoInformation> lstFilteredVideoInformation = [];

        foreach (VideoInformation videoInfo in topVideoList)
        {
            dictRecord.TryGetValue(videoInfo.Id, out VTuberRecord? vTuberRecord);

            if (vTuberFilterOption != null)
            {
                if (vTuberRecord == null)
                {
                    continue;
                }

                if (!MatchFilter(vTuberRecord.DebutDate, vTuberFilterOption))
                {
                    continue;
                }
            }

            lstFilteredVideoInformation.Add(videoInfo);
        }

        TopVideosList filteredTopVideosList = new();
        filteredTopVideosList.Insert(lstFilteredVideoInformation);

        IEnumerable<VideoInformation> lstVideoInformation;
        if (allowDuplicate)
        {
            lstVideoInformation = filteredTopVideosList.GetSortedList();
        }
        else
        {
            lstVideoInformation = filteredTopVideosList.GetNoDuplicateList();
        }

        List<VTuberId> lstValidVTubers = [.. dictRecord.Keys];

        foreach (
            VideoInformation videoInfo in lstVideoInformation
                .Where(p => lstValidVTubers.Contains(p.Id))
                .Where(p => dictRecord[p.Id].Nationality.Contains(NationalityFilter))
                .OrderByDescending(e => e.ViewCount)
                .Take(count)
        )
        {
            VTuberRecord record = dictRecord[videoInfo.Id];

            VideoPopularityData videoData =
                new(
                    id: record.Id,
                    name: record.DisplayName,
                    imgUrl: record.ImageUrl,
                    title: videoInfo.Title,
                    videoUrl: videoInfo.Url,
                    thumbnailUrl: MiscUtils.SetTwitchThumbnailUrlSize(
                        videoInfo.ThumbnailUrl,
                        width: 320,
                        height: 180
                    ),
                    viewCount: videoInfo.ViewCount,
                    uploadTime: MiscUtils.ToIso8601UtcString(videoInfo.PublishDateTime)
                );

            rLst.Add(videoData);
        }

        return rLst;
    }

    private static bool MatchFilter(DateOnly? debutDate, VTuberFilterOption vTuberFilterOption)
    {
        // consider VTubers without debut date to be before any date
        if (debutDate == null)
        {
            switch (vTuberFilterOption.FilterOption)
            {
                case FilterOption.Before:
                    return true;
                case FilterOption.AfterOrEqual:
                    return false;
            }
        }

        return vTuberFilterOption.FilterOption switch
        {
            FilterOption.Before => debutDate < vTuberFilterOption.DebutDate,
            FilterOption.AfterOrEqual => debutDate >= vTuberFilterOption.DebutDate,
            _ => false,
        };
    }
}
