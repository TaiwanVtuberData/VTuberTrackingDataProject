using Common.Types;
using GenerateJsonFile.Types;

namespace GenerateJsonFile;

class TopVideosListToJsonStruct
{
    public readonly string NationalityFilter;

    public TopVideosListToJsonStruct(string nationalityFilter)
    {
        NationalityFilter = nationalityFilter;
    }

    private static string SetTwitchThumbnailUrlSize(string str, int width, int height)
    {
        if (!str.Contains("%{width}") || !str.Contains("%{height}"))
            return str;

        return str
            .Replace("%{width}", width.ToString())
            .Replace("%{height}", height.ToString());
    }

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

            return x.Owner == y.Owner;
        }

        int IEqualityComparer<VideoInformation>.GetHashCode(VideoInformation obj)
        {
            return obj.GetHashCode();
        }
    }
    public List<VideoPopularityData> Get(TrackList trackList, TopVideosList topVideoList, DictionaryRecord dictRecord, int count, bool allowDuplicate)
    {
        List<VideoPopularityData> rLst = new();

        IEnumerable<VideoInformation> lstVideoInformation;
        if (allowDuplicate)
        {
            lstVideoInformation = topVideoList.GetSortedList();
        }
        else
        {
            lstVideoInformation = topVideoList.GetNoDuplicateList();
        }

        foreach (VideoInformation videoInfo in lstVideoInformation
            .Where(p => dictRecord[trackList.GetDisplayName(p.Owner)].Nationality.Contains(NationalityFilter))
            .OrderByDescending(e => e.ViewCount)
            .Take(count))
        {
            string displayName = trackList.GetDisplayName(videoInfo.Owner);

            VTuberRecord record = dictRecord[displayName];

            VideoPopularityData videoData = new()
            {
                id = record.Id,
                name = record.DisplayName,
                imgUrl = record.ThumbnailUrl,
                title = videoInfo.Title,
                videoUrl = videoInfo.Url,
                thumbnailUrl = SetTwitchThumbnailUrlSize(videoInfo.ThumbnailUrl, width: 320, height: 180),
                viewCount = videoInfo.ViewCount,
                // https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-date-and-time-format-strings#Roundtrip
                // use "o" specifier to get correct format 2009-06-15T13:45:30.0000000Z
                uploadTime = videoInfo.PublishDateTime.ToString("o"),
            };

            rLst.Add(videoData);
        }

        return rLst;
    }
}
