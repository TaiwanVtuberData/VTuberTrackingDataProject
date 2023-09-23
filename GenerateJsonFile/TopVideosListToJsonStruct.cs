using Common.Types;
using Common.Types.Basic;
using GenerateJsonFile.Types;
using GenerateJsonFile.Utils;

namespace GenerateJsonFile;

class TopVideosListToJsonStruct {
    public readonly string NationalityFilter;

    public TopVideosListToJsonStruct(string nationalityFilter) {
        NationalityFilter = nationalityFilter;
    }

    class OwnerComparer : IEqualityComparer<VideoInformation> {
        public bool Equals(VideoInformation? x, VideoInformation? y) {
            if (x is null && y is null)
                return false;

            if (x is null)
                return false;

            if (y is null)
                return false;

            return x.Id == y.Id;
        }

        int IEqualityComparer<VideoInformation>.GetHashCode(VideoInformation obj) {
            return obj.GetHashCode();
        }
    }
    public List<VideoPopularityData> Get(TopVideosList topVideoList, DictionaryRecord dictRecord, int count, bool allowDuplicate) {
        List<VideoPopularityData> rLst = new();

        IEnumerable<VideoInformation> lstVideoInformation;
        if (allowDuplicate) {
            lstVideoInformation = topVideoList.GetSortedList();
        } else {
            lstVideoInformation = topVideoList.GetNoDuplicateList();
        }

        List<VTuberId> lstValidVTubers = dictRecord.Keys.ToList();

        foreach (VideoInformation videoInfo in lstVideoInformation
            .Where(p => lstValidVTubers.Contains(p.Id))
            .Where(p => dictRecord[p.Id].Nationality.Contains(NationalityFilter))
            .OrderByDescending(e => e.ViewCount)
            .Take(count)) {
            VTuberRecord record = dictRecord[videoInfo.Id];

            VideoPopularityData videoData = new(
                id: record.Id,
                name: record.DisplayName,
                imgUrl: record.ImageUrl,
                title: videoInfo.Title,
                videoUrl: videoInfo.Url,
                thumbnailUrl: MiscUtils.SetTwitchThumbnailUrlSize(videoInfo.ThumbnailUrl, width: 320, height: 180),
                viewCount: videoInfo.ViewCount,
                uploadTime: MiscUtils.ToIso8601UtcString(videoInfo.PublishDateTime)
            );

            rLst.Add(videoData);
        }

        return rLst;
    }
}
