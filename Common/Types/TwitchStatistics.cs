using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Types;
public class TwitchStatistics
{
    public ulong FollowerCount { get; private set; } = 0;
    public ulong RecentMedianViewCount { get; set; } = 0;
    public ulong RecentHighestViewCount { get; set; } = 0;
    public ulong RecentPopularity { get; set; } = 0;
    public string HighestViewedVideoURL { get; set; } = "";
    public decimal FollowerCountToMedianViewCount { get; private set; } = 0m;
    public decimal FollowerCountToPopularity { get; private set; } = 0m;

    public TwitchStatistics()
    {
        UpdateFollowerCount(0);
    }

    public void UpdateFollowerCount(ulong followerCount)
    {
        FollowerCount = followerCount;

        if (FollowerCount == 0)
        {
            FollowerCountToMedianViewCount = 0m;
        }
        else
        {
            FollowerCountToMedianViewCount = (decimal)RecentMedianViewCount / FollowerCount * 100m;
            FollowerCountToPopularity = (decimal)RecentPopularity / FollowerCount * 100m;
        }
    }
}
