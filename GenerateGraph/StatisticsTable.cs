using System.Data;
using Common.Types;
using Common.Types.Basic;

namespace GenerateGraph;

// column: Channel name
// row: DateTime
class StatisticsTable : DataTable
{
    private readonly TrackList _TrackList;
    private readonly List<DateTimeOffset> _RowDateTime;

    public StatisticsTable(TrackList trackList, bool byGroup)
    {
        _TrackList = trackList;
        _RowDateTime = [];

        if (byGroup)
        {
            foreach (string groupName in _TrackList.GetGroupNameList())
            {
                this.Columns.Add(groupName, typeof(VTuberStatistics));
            }
        }
        else
        {
            foreach (VTuberId id in _TrackList.GetIdList())
            {
                this.Columns.Add(id.Value, typeof(VTuberStatistics));
            }
        }
    }

    public void AddRow(
        DateTimeOffset dateTime,
        Dictionary<VTuberId, VTuberStatistics> statisticsDict
    )
    {
        _RowDateTime.Add(dateTime);

        DataRow dataRow = this.NewRow();
        foreach (KeyValuePair<VTuberId, VTuberStatistics> channelStat in statisticsDict)
        {
            VTuberId channelName = channelStat.Key;
            VTuberStatistics stat = channelStat.Value;

            if (this.Columns.Contains(channelName.Value))
            {
                dataRow[channelName.Value] = stat;
            }
        }

        this.Rows.Add(dataRow);
    }

    public void FillEmptyValueByInterpolation()
    {
        int rowCount = this.Rows.Count;
        foreach (DataColumn column in this.Columns)
        {
            int index = 0;
            foreach (DataRow row in this.Rows)
            {
                // only iterate through [1,rowCount-2]
                if (index == 0 || index == rowCount - 1)
                {
                    goto END_LABEL;
                }

                object currentStatObject = row[column.ColumnName];
                if (currentStatObject.GetType() != typeof(VTuberStatistics))
                {
                    Tuple<int, int, VTuberStatistics, VTuberStatistics>? nearestIndex =
                        GetNearestStatistics(column, index);
                    if (nearestIndex != null)
                    {
                        int nearestPre = nearestIndex.Item1;
                        int nearestPost = nearestIndex.Item2;
                        VTuberStatistics nearestPreStat = nearestIndex.Item3;
                        VTuberStatistics nearestPostStat = nearestIndex.Item4;

                        VTuberStatistics interpolationStat = GetStatisticsByInterpolation(
                            targetDateTime: _RowDateTime[index],
                            preDateTime: _RowDateTime[nearestPre],
                            postDateTime: _RowDateTime[nearestPost],
                            nearestPreStat,
                            nearestPostStat
                        );

                        row[column.ColumnName] = interpolationStat;
                    }
                }

                END_LABEL:
                index++;
            }
        }
    }

    public List<DateTimeOffset> GetDateTimeList()
    {
        return new(_RowDateTime);
    }

    private static object? GetPropValue(object? obj, string propertyName)
    {
        foreach (string part in propertyName.Split('.'))
        {
            if (obj == null)
            {
                return null;
            }

            Type type = obj.GetType();
            System.Reflection.PropertyInfo? info = type.GetProperty(part);
            if (info == null)
            {
                return null;
            }

            obj = info.GetValue(obj, null);
        }
        return obj;
    }

    public Dictionary<VTuberId, List<decimal>> GetStatisticDictByField(
        string fieldName,
        decimal? youTubeSubscriberCountConstriant
    )
    {
        // initialize capacity, not size
        Dictionary<VTuberId, List<decimal>> rList = new(_RowDateTime.Count);

        if (!youTubeSubscriberCountConstriant.HasValue)
        {
            foreach (DataColumn column in this.Columns)
            {
                rList.Add(new VTuberId(column.ColumnName), []);
            }
        }
        else
        {
            DataRow lastRow = this.Rows[this.Rows.Count - 1];

            int index = 0;
            foreach (DataColumn column in this.Columns)
            {
                object statObj = lastRow[index];

                if (statObj.GetType() == typeof(VTuberStatistics))
                {
                    decimal value = Convert.ToDecimal(
                        GetPropValue(statObj, "YouTube.Basic.SubscriberCount")
                    );

                    if (value >= youTubeSubscriberCountConstriant)
                        rList.Add(new VTuberId(column.ColumnName), []);
                }

                index++;
            }
        }

        foreach (DataRow row in this.Rows)
        {
            foreach (DataColumn column in this.Columns)
            {
                object statObj = row[column.ColumnName];

                if (rList.ContainsKey(new VTuberId(column.ColumnName)))
                {
                    if (statObj.GetType() == typeof(VTuberStatistics))
                    {
                        decimal value = Convert.ToDecimal(GetPropValue(statObj, fieldName));
                        rList[new VTuberId(column.ColumnName)].Add(value);
                    }
                    else
                    {
                        rList[new VTuberId(column.ColumnName)].Add(0m);
                    }
                }
            }
        }

        return rList;
    }

    private Tuple<int, int, VTuberStatistics, VTuberStatistics>? GetNearestStatistics(
        DataColumn targetColumn,
        int targetIndex
    )
    {
        string targetColumnName = targetColumn.ColumnName;

        int nearestPre = int.MinValue;
        int nearestPost = int.MaxValue;

        VTuberStatistics? nearestPreStat = null;
        VTuberStatistics? nearestPostStat = null;

        int index = 0;
        foreach (DataRow row in this.Rows)
        {
            if (index == targetIndex)
            {
                goto END_LABEL;
            }

            object currentStatObject = row[targetColumnName];
            if (currentStatObject.GetType() == typeof(VTuberStatistics))
            {
                if (index > nearestPre && index < targetIndex)
                {
                    nearestPre = index;
                    nearestPreStat = (VTuberStatistics)currentStatObject;
                }

                if (index < nearestPost && index > targetIndex)
                {
                    nearestPost = index;
                    nearestPostStat = (VTuberStatistics)currentStatObject;
                }
            }

            END_LABEL:
            index++;
        }

        if (nearestPreStat != null && nearestPostStat != null)
            return new(nearestPre, nearestPost, nearestPreStat, nearestPostStat);
        else
            return null;
    }

    private static VTuberStatistics GetStatisticsByInterpolation(
        DateTimeOffset targetDateTime,
        DateTimeOffset preDateTime,
        DateTimeOffset postDateTime,
        VTuberStatistics preStat,
        VTuberStatistics postStat
    )
    {
        TimeSpan preInterval = targetDateTime - preDateTime;
        TimeSpan postInterval = postDateTime - targetDateTime;

        decimal preRatio = (decimal)(
            postInterval.TotalMilliseconds / (preInterval + postInterval).TotalMilliseconds
        );

        return VTuberStatistics.GenerateStatisticsByInterpolation(preRatio, preStat, postStat);
    }
}
