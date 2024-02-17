using System.Globalization;

namespace GenerateRecordList.Utils;
public class TimeUtils {
    private static readonly GregorianCalendar calendar = new();

    public static bool IsBetween(DateOnly? maybeDate, DateOnly dateBefore, DateOnly dateAfter) {
        if (maybeDate == null) {
            return false;
        }

        DateOnly date = maybeDate.Value;

        return IsBetween(date, dateBefore, dateAfter);
    }

    public static bool GetAnniversaryYearByRange(DateOnly? maybeDate, DateOnly dateBefore, DateOnly dateAfter, out uint anniversaryYearCount) {
        anniversaryYearCount = 0;

        if (maybeDate == null) {
            return false;
        }

        DateOnly date = maybeDate.Value;
        List<int> yearList = GetYearList(dateBefore.Year, dateAfter.Year);

        foreach (int year in yearList) {
            // It would not be considered to be an anniversary if the date has not happed yet.
            if (date.Year >= year) {
                continue;
            }

            // Check if the `date` is a leap day. Ex: 2024-02-29
            // and check if `today` is not a leap year. Ex: 2025
            // If the above checks are true then set `dateThisYear` to 2025-02-28.
            // Otherwise just set `dateThisYear` to `today.Year`.
            DateOnly dateThisYear;
            if (calendar.IsLeapDay(date.Year, date.Month, date.Day) &&
                !calendar.IsLeapYear(year)
                ) {
                dateThisYear = new(year, 2, 28);
            } else {
                dateThisYear = new(year, date.Month, date.Day);
            }

            bool isBetween = IsBetween(dateThisYear, dateBefore, dateAfter);
            if (isBetween) {
                anniversaryYearCount = (uint)(year - date.Year);
                return true;
            }

            // keep checking if `dateThisYear` is not in range
        }

        // return false if all possible year is checked
        return false;
    }

    private static bool IsBetween(DateOnly date, DateOnly dateBefore, DateOnly dateAfter) {
        return dateBefore <= date && date <= dateAfter;
    }

    private static List<int> GetYearList(int inclusiveStart, int inclusiveEnd) {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(inclusiveStart, inclusiveEnd);

        return Enumerable.Range(
            start: inclusiveStart,
            count: inclusiveEnd - inclusiveStart + 1
            ).ToList();
    }
}
