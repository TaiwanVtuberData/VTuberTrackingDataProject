using System.Xml;

namespace Common.Types;
public class TopVideosList {

  readonly List<VideoInformation> InternalList;
  readonly int MaxListCount;
  public TopVideosList(int videoCount = int.MaxValue) {
    MaxListCount = Math.Max(1, videoCount);
    InternalList = new();
  }

  public IEnumerator<VideoInformation> GetEnumerator() {
    return InternalList.GetEnumerator();
  }

  public List<VideoInformation> GetSortedList() {
    List<VideoInformation> rLst = new(InternalList);
    rLst.Reverse();

    return rLst;
  }

  public List<VideoInformation> GetNoDuplicateList() {
    List<VideoInformation> rLst = new(capacity: InternalList.Count);

    foreach (VideoInformation videoInfo in GetSortedList().OrderByDescending(e => e.ViewCount)) {
      // if list already contains videoInfo.Owner
      if (rLst.Any(e => e.Id == videoInfo.Id)) {
        continue;
      }

      rLst.Add(videoInfo);
    }

    return rLst;
  }

  public void Insert(string[] entryBlock) {
    Insert(new VideoInformation() {
      Id = entryBlock[0],
      ViewCount = ulong.Parse(entryBlock[1]),
      Title = entryBlock[2],
      PublishDateTime = XmlConvert.ToDateTime(entryBlock[3], XmlDateTimeSerializationMode.Utc),
      Url = entryBlock[4],
      ThumbnailUrl = entryBlock[5],
    }
    );
  }

  // https://stackoverflow.com/a/22801345/11947017
  public void Insert(VideoInformation videoInformation) {
    if (InternalList.Count == 0) {
      InternalList.Add(videoInformation);
      goto REMOVE_EXTRA;
    }

    if (InternalList.Last().CompareTo(videoInformation) <= 0) {
      InternalList.Add(videoInformation);
      goto REMOVE_EXTRA;
    }

    if (InternalList.First().CompareTo(videoInformation) >= 0) {
      InternalList.Insert(0, videoInformation);
      goto REMOVE_EXTRA;
    }

    int index = InternalList.BinarySearch(videoInformation);
    if (index < 0) {
      index = ~index;
    }

    InternalList.Insert(index, videoInformation);

  REMOVE_EXTRA:
    // InternalList should only contain at most (MaxListCount + 1) element
    if (InternalList.Count > MaxListCount) {
      InternalList.RemoveRange(0, (InternalList.Count - MaxListCount));
    }
  }
}