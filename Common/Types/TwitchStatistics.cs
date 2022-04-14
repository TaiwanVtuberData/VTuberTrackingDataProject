using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Types;
public class TwitchStatistics
{
    public ulong FollowerCount { get; set; } = 0;
    public ulong RecentMedianViewCount { get; set; } = 0;
    public ulong RecentHighestViewCount { get; set; } = 0;
    public string HighestViewedVideoURL { get; set; } = "";
    public decimal FollowerCountToMedianViewCount { get; set; } = 0m;

    public TwitchStatistics()
    {
        UpdateFollowerCountToMedianViewCount();
    }

    public void UpdateFollowerCountToMedianViewCount()
    {
        if (FollowerCount == 0)
            FollowerCountToMedianViewCount = 0m;
        else
            FollowerCountToMedianViewCount = (decimal)RecentMedianViewCount / FollowerCount * 100m;
    }
}
