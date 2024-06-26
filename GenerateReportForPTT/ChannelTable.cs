﻿using System.Data;
using System.Globalization;

namespace GenerateReportForPTT;
class ChannelTable : DataTable {
    // default header
    private static class HeaderIndex {
        public const int channelName = 0;
        public const int valueCount = 1;
        public const int lastWeekDifference = 2;
        public const int lastMonthDifference = 3;
        public const int remark = 4;
        // invisible column
        public const int increasedValueByWeek = 5;
        public const int increasedPercengateByWeek = 6;
    }

    private readonly string NO_RECORD_STRING = "(無紀錄)";

    private static readonly NumberFormatInfo FormatInfo = GetNumberFormatInfo();
    private static NumberFormatInfo GetNumberFormatInfo() {
        NumberFormatInfo formatInfo = new() {
            PositiveSign = "+",
            NegativeSign = "-",
            NumberGroupSeparator = ",",
            NumberGroupSizes = [4]
        };
        return formatInfo;
    }

    public bool SortByIncreasePercentage { get; private set; }
    public bool OnlyShowValueChanges { get; private set; }

    public ChannelTable(string valueHeader, bool sortByIncreasePercentage, bool onlyShowValueChanges) {
        this.Columns.Add("頻道名稱", typeof(StringWithColor));
        this.Columns.Add(valueHeader, typeof(string));
        this.Columns.Add("上週增減", typeof(StringWithColor));
        this.Columns.Add("上月增減", typeof(StringWithColor));
        this.Columns.Add("備註", typeof(StringWithColor));
        this.Columns.Add("Increased Value By Week(Do not print)", typeof(decimal));
        this.Columns.Add("Increased Percentage By Week(Do not print)", typeof(string));

        SortByIncreasePercentage = sortByIncreasePercentage;
        OnlyShowValueChanges = onlyShowValueChanges;
    }


    private static string ToThisFormat(object obj, bool addPostiveSign) {
        string ans = string.Format(FormatInfo, "{0:N0}", obj);

        if (addPostiveSign) {
            if (obj.GetType() == typeof(decimal)) {
                if ((decimal)obj > 0m)
                    ans = '+' + ans;
            }
        }

        return ans;
    }

    public void AddChannel(string channelName, decimal currentValue, decimal? lastWeekValue, decimal? lastMonthValue, bool isLesserThanLastWeek, string remarkText) {
        // (空三格)(空二格)頻道名稱(至少空二格)觀看中位數(至少空二格)上週增減(至少空二格)上月增減(空二格)備註
        //      2.        香草奈若              1,5906            + 2630             + 5000  24 天前首次直播

        decimal lastWeekDifference = (currentValue - lastWeekValue).GetValueOrDefault();
        decimal lastMonthDifference = (currentValue - lastMonthValue).GetValueOrDefault();

        bool addPostiveSign = !OnlyShowValueChanges;

        string lastWeekDifferenceString = lastWeekValue == null ? NO_RECORD_STRING : ToThisFormat(lastWeekDifference, addPostiveSign);
        string lastMonthDifferenceString = lastMonthValue == null ? NO_RECORD_STRING : ToThisFormat(lastMonthDifference, addPostiveSign);

        ColorCode lastWeekDifferenceColor = ColorCode.NO_COLOR;
        ColorCode lastMonthDifferenceColor = ColorCode.NO_COLOR;

        if (!OnlyShowValueChanges) {
            if (lastWeekValue * 0.05m < lastWeekDifference)
                lastWeekDifferenceColor = ColorCode.RED;
            else if (lastWeekValue * 0.05m < -lastWeekDifference)
                lastWeekDifferenceColor = ColorCode.GREEN;

            if (currentValue * 0.1m < lastMonthDifference)
                lastMonthDifferenceColor = ColorCode.RED;
            else if (currentValue * 0.1m < -lastMonthDifference)
                lastMonthDifferenceColor = ColorCode.GREEN;
        }

        decimal increaseValue;
        if (SortByIncreasePercentage) {
            if (currentValue != 0m)
                increaseValue = (lastWeekDifference / currentValue) * 100;
            else
                increaseValue = 0m;
        } else {
            increaseValue = lastWeekDifference;
        }

        ColorCode channelColor = ColorCode.NO_COLOR;
        if (isLesserThanLastWeek) {
            channelColor = ColorCode.YELLOW;
        }

        string increasePercentageString = ToThisFormat(increaseValue, false) + '%';

        this.Rows.Add(new StringWithColor(channelName, channelColor),
            ToThisFormat(currentValue, false),
            new StringWithColor(lastWeekDifferenceString, lastWeekDifferenceColor),
            new StringWithColor(lastMonthDifferenceString, lastMonthDifferenceColor),
            new StringWithColor(remarkText, ColorCode.NO_COLOR),
            increaseValue,
            increasePercentageString);
    }

    public string ToString(int maxColumnLength) {
        int rankSpace = 4;
        int spacing1 = 2;
        string rankChannelNameSpace = new(' ', spacing1);

        string ans = "";

        if (OnlyShowValueChanges) {
            List<int> ColumnToPrintIndexes =
            [
                HeaderIndex.channelName,
                HeaderIndex.lastWeekDifference,
                HeaderIndex.lastMonthDifference,
                HeaderIndex.remark,
            ];

            List<int> columnsOccupySpace = GetColumnsOccupiedSpace(ColumnToPrintIndexes);

            if (maxColumnLength > 0)
                columnsOccupySpace[0] = maxColumnLength;

            ans += HeaderString(rankSpace, columnsOccupySpace, ColumnToPrintIndexes);
            ans += "\r\n"; // PCMan only accept \r\n new line

            DataView dataView = this.DefaultView;
            dataView.Sort = "Increased Value By Week(Do not print) desc";

            DataTable sortedTable = dataView.ToTable();
            int rank = 1;
            foreach (DataRow row in sortedTable.Rows) {
                ans += GetRankString(rank, rankSpace);

                ans += rankChannelNameSpace;
                ans += GetUnicodeAwarePaddedString(
                    row[HeaderIndex.channelName].ToString(),
                    columnsOccupySpace[0],
                    Justify.left,
                    ((StringWithColor)row[HeaderIndex.channelName]).Color);
                ans += "  "; // 2 spaces
                ans += GetUnicodeAwarePaddedString(
                    row[HeaderIndex.lastWeekDifference].ToString(),
                    columnsOccupySpace[1],
                    Justify.right,
                    ((StringWithColor)row[HeaderIndex.lastWeekDifference]).Color);
                ans += "  "; // 2 spaces
                ans += GetUnicodeAwarePaddedString(
                    row[HeaderIndex.lastMonthDifference].ToString(),
                    columnsOccupySpace[2],
                    Justify.right,
                    ((StringWithColor)row[HeaderIndex.lastMonthDifference]).Color);
                ans += "  "; // 2 spaces
                ans += GetUnicodeAwarePaddedString(
                    row[HeaderIndex.remark].ToString(),
                    columnsOccupySpace[3],
                    Justify.left,
                    ((StringWithColor)row[HeaderIndex.remark]).Color,
                    trimTrailing: true);
                ans += "\r\n"; // PCMan only accept \r\n new line

                rank++;
            }
        } else {
            if (SortByIncreasePercentage) {
                List<int> ColumnToPrintIndexes =
                [
                    HeaderIndex.channelName,
                    HeaderIndex.valueCount,
                    HeaderIndex.lastWeekDifference,
                    HeaderIndex.lastMonthDifference,
                    HeaderIndex.remark,
                ];

                List<int> columnsOccupySpace = GetColumnsOccupiedSpace(ColumnToPrintIndexes);

                if (maxColumnLength > 0) {
                    columnsOccupySpace[0] = maxColumnLength;
                }

                ans += HeaderString(rankSpace, columnsOccupySpace, ColumnToPrintIndexes);
                ans += "\r\n"; // PCMan only accept \r\n new line

                DataView dataView = this.DefaultView;
                dataView.Sort = "Increased Value By Week(Do not print) desc";

                DataTable sortedTable = dataView.ToTable();
                int rank = 1;
                foreach (DataRow row in sortedTable.Rows) {
                    ans += GetUnicodeAwarePaddedString(
                        row[HeaderIndex.increasedPercengateByWeek].ToString(),
                        4,
                        Justify.right,
                        ColorCode.NO_COLOR);

                    ans += rankChannelNameSpace;
                    ans += GetUnicodeAwarePaddedString(
                        row[HeaderIndex.channelName].ToString(),
                        columnsOccupySpace[0],
                        Justify.left,
                        ((StringWithColor)row[HeaderIndex.channelName]).Color);
                    ans += "  "; // 2 spaces
                    ans += GetUnicodeAwarePaddedString(
                        row[HeaderIndex.valueCount].ToString(),
                        columnsOccupySpace[1],
                        Justify.right,
                        ColorCode.NO_COLOR);
                    ans += "  "; // 2 spaces
                    ans += GetUnicodeAwarePaddedString(
                        row[HeaderIndex.lastWeekDifference].ToString(),
                        columnsOccupySpace[2],
                        Justify.right,
                        ((StringWithColor)row[HeaderIndex.lastWeekDifference]).Color);
                    ans += "  "; // 2 spaces
                    ans += GetUnicodeAwarePaddedString(
                        row[HeaderIndex.lastMonthDifference].ToString(),
                        columnsOccupySpace[3],
                        Justify.right,
                        ((StringWithColor)row[HeaderIndex.lastMonthDifference]).Color);
                    ans += "  "; // 2 spaces
                    ans += GetUnicodeAwarePaddedString(
                        row[HeaderIndex.remark].ToString(),
                        columnsOccupySpace[4],
                        Justify.left,
                        ((StringWithColor)row[HeaderIndex.remark]).Color,
                        trimTrailing: true);
                    ans += "\r\n"; // PCMan only accept \r\n new line

                    rank++;
                }
            } else {
                List<int> ColumnToPrintIndexes =
                [
                    HeaderIndex.channelName,
                    HeaderIndex.valueCount,
                    HeaderIndex.lastWeekDifference,
                    HeaderIndex.lastMonthDifference,
                    HeaderIndex.remark,
                ];

                List<int> columnsOccupySpace = GetColumnsOccupiedSpace(ColumnToPrintIndexes);

                if (maxColumnLength > 0) {
                    columnsOccupySpace[0] = maxColumnLength;
                }

                ans += HeaderString(rankSpace, columnsOccupySpace, ColumnToPrintIndexes);
                ans += "\r\n"; // PCMan only accept \r\n new line

                DataView dataView = this.DefaultView;

                DataTable sortedTable = dataView.ToTable();
                int rank = 1;
                foreach (DataRow row in sortedTable.Rows) {
                    ans += GetRankString(rank, rankSpace);

                    ans += rankChannelNameSpace;
                    ans += GetUnicodeAwarePaddedString(
                        row[HeaderIndex.channelName].ToString(),
                        columnsOccupySpace[0],
                        Justify.left,
                        ((StringWithColor)row[HeaderIndex.channelName]).Color);
                    ans += "  "; // 2 spaces
                    ans += GetUnicodeAwarePaddedString(
                        row[HeaderIndex.valueCount].ToString(),
                        columnsOccupySpace[1],
                        Justify.right,
                        ColorCode.NO_COLOR);
                    ans += "  "; // 2 spaces
                    ans += GetUnicodeAwarePaddedString(
                        row[HeaderIndex.lastWeekDifference].ToString(),
                        columnsOccupySpace[2],
                        Justify.right,
                        ((StringWithColor)row[HeaderIndex.lastWeekDifference]).Color);
                    ans += "  "; // 2 spaces
                    ans += GetUnicodeAwarePaddedString(
                        row[HeaderIndex.lastMonthDifference].ToString(),
                        columnsOccupySpace[3],
                        Justify.right,
                        ((StringWithColor)row[HeaderIndex.lastMonthDifference]).Color);
                    ans += "  "; // 2 spaces
                    ans += GetUnicodeAwarePaddedString(
                        row[HeaderIndex.remark].ToString(),
                        columnsOccupySpace[4],
                        Justify.left,
                        ((StringWithColor)row[HeaderIndex.remark]).Color,
                        trimTrailing: true);
                    ans += "\r\n"; // PCMan only accept \r\n new line

                    rank++;
                }
            }
        }

        return ans;
    }

    private string HeaderString(int rankOccupiedSpace, List<int> columnsOccupySpace, List<int> headerIndexes) {
        string ans = new(' ', rankOccupiedSpace);

        int i = 0;
        foreach (int headerIndex in headerIndexes) {
            ans += "  "; // 2 spaces
            ans += GetUnicodeAwarePaddedString(
                this.Columns[headerIndex].ColumnName,
                columnsOccupySpace[i],
                Justify.left,
                ColorCode.NO_COLOR);
            i++;
        };

        return ans;
    }

    private List<int> GetColumnsOccupiedSpace(List<int> headerIndexes) {
        List<int> ans = new(capacity: headerIndexes.Count); // initialize the capacity, not the size
        foreach (int headerIndex in headerIndexes) {
            ans.Add(GetColumnOccupiedSpace(headerIndex));
        }

        return ans;
    }

    private int GetColumnOccupiedSpace(int index) {
        DataColumn column = this.Columns[index];
        string headerString = column.ColumnName;
        int maxOccupiedSpace = GetOccupiedSpace(headerString);
        foreach (DataRow row in this.Rows) {
            string? currentString = row[index].ToString();
            if (currentString is null) {
                continue;
            }

            int currentOccupiedSpace = GetOccupiedSpace(currentString);
            if (currentOccupiedSpace > maxOccupiedSpace)
                maxOccupiedSpace = currentOccupiedSpace;
        }

        return maxOccupiedSpace;
    }

    private static string GetRankString(int rank, int occupicedSpace) {
        return $"{rank}.".PadLeft(occupicedSpace);
    }

    private static int GetOccupiedSpace(string str) {
        int occupiedSpace = 0;
        foreach (char c in str) {
            // Uncode character occupy 2 spaces while ANSI character occupy 1 space
            occupiedSpace += ContainsUnicodeCharacter(c) ? 2 : 1;
        }
        return occupiedSpace;
    }

    private static bool ContainsUnicodeCharacter(char input) {
        const char MaxAnsiCode = (char)255;

        return input > MaxAnsiCode;
    }

    public enum Justify {
        left,
        right
    }
    public static string GetUnicodeAwarePaddedString(string str, int space, Justify justify, ColorCode colorCode, bool trimTrailing = false) {
        int strSpace = GetOccupiedSpace(str);

        char ESC_ASCII = (char)27;
        switch (colorCode) {
            case ColorCode.NO_COLOR:
                break;
            case ColorCode.RED:
                str = ESC_ASCII + "[1;31m" + ESC_ASCII + ' ' + str + ESC_ASCII + "[m";
                break;
            case ColorCode.GREEN:
                str = ESC_ASCII + "[1;32m" + ESC_ASCII + ' ' + str + ESC_ASCII + "[m";
                break;
            case ColorCode.YELLOW:
                str = ESC_ASCII + "[1;33m" + ESC_ASCII + ' ' + str + ESC_ASCII + "[m";
                break;
        }

        int actualSpace = Math.Max(space - strSpace, 0);

        if (trimTrailing) {
            return justify switch {
                Justify.left => str,
                Justify.right => new string(' ', actualSpace) + str,
                _ => throw new Exception("Unhandled Justify style"),
            };
        } else {
            return justify switch {
                Justify.left => str + new string(' ', actualSpace),
                Justify.right => new string(' ', actualSpace) + str,
                _ => throw new Exception("Unhandled Justify style"),
            };
        }
    }
}