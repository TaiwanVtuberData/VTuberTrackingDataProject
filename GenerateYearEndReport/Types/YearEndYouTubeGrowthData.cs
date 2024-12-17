using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GenerateRecordList.Types;

namespace GenerateYearEndReport.Types;

public record YearEndYouTubeGrowthData(
    string id,
    BaseCountType subscriber,
    GrowthData _365DaysGrowth,
    string? Nationality
) : YouTubeData(id: id, subscriber: subscriber);
