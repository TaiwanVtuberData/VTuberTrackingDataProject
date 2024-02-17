using GenerateRecordList.Utils;

namespace GenerateRecordListTest;

[TestClass]
public class TimeUtilsTest {
    [TestMethod]
    public void GetAnniversaryYearByRange_True() {
        DateOnly? date = new(year: 2022, month: 12, day: 31);
        DateOnly dateBefore = new(year: 2023, month: 12, day: 15);
        DateOnly dateAfter = new(year: 2024, month: 1, day: 15);

        bool result = TimeUtils.GetAnniversaryYearByRange(
            maybeDate: date,
            dateBefore: dateBefore,
            dateAfter: dateAfter,
            anniversaryYearCount: out uint anniversaryYearCount
            );

        Assert.AreEqual(
            expected: true,
            actual: result
            );

        Assert.AreEqual(
            expected: 1u,
            actual: anniversaryYearCount
            );
    }

    [TestMethod]
    public void GetAnniversaryYearByRange_TwoYears_True() {
        DateOnly? date = new(year: 2021, month: 12, day: 31);
        DateOnly dateBefore = new(year: 2023, month: 12, day: 15);
        DateOnly dateAfter = new(year: 2024, month: 1, day: 15);

        bool result = TimeUtils.GetAnniversaryYearByRange(
            maybeDate: date,
            dateBefore: dateBefore,
            dateAfter: dateAfter,
            anniversaryYearCount: out uint anniversaryYearCount
            );

        Assert.AreEqual(
            expected: true,
            actual: result
            );

        Assert.AreEqual(
            expected: 2u,
            actual: anniversaryYearCount
            );
    }

    [TestMethod]
    public void GetAnniversaryYearByRange_LesserThanOneYear_False() {
        DateOnly? date = new(year: 2023, month: 12, day: 31);
        DateOnly dateBefore = new(year: 2023, month: 12, day: 15);
        DateOnly dateAfter = new(year: 2024, month: 1, day: 15);

        bool result = TimeUtils.GetAnniversaryYearByRange(
            maybeDate: date,
            dateBefore: dateBefore,
            dateAfter: dateAfter,
            anniversaryYearCount: out uint anniversaryYearCount
            );

        Assert.AreEqual(
            expected: false,
            actual: result
            );
    }

    [TestMethod]
    public void GetAnniversaryYearByRange_False() {
        DateOnly? date = new(year: 2023, month: 12, day: 14);
        DateOnly dateBefore = new(year: 2023, month: 12, day: 15);
        DateOnly dateAfter = new(year: 2024, month: 1, day: 15);

        bool result = TimeUtils.GetAnniversaryYearByRange(
            maybeDate: date,
            dateBefore: dateBefore,
            dateAfter: dateAfter,
            anniversaryYearCount: out uint anniversaryYearCount
            );

        Assert.AreEqual(
            expected: false,
            actual: result
            );
    }

    [TestMethod]
    public void GetAnniversaryYearByRange_LeapDay_True() {
        // Consider leap day 02-29 to be the same as 02-28 so that
        // leap day would show up each year
        DateOnly? date = new(year: 2024, month: 2, day: 29);
        DateOnly dateBefore = new(year: 2025, month: 2, day: 28);
        DateOnly dateAfter = new(year: 2025, month: 3, day: 1);

        bool result = TimeUtils.GetAnniversaryYearByRange(
            maybeDate: date,
            dateBefore: dateBefore,
            dateAfter: dateAfter,
            anniversaryYearCount: out uint anniversaryYearCount
            );

        Assert.AreEqual(
            expected: true,
            actual: result
            );

        Assert.AreEqual(
            expected: 1u,
            actual: anniversaryYearCount
            );
    }

    [TestMethod]
    public void GetAnniversaryYearByRange_LeapDayLesserThanOneYear_False() {
        // Consider leap day 02-29 to be the same as 02-28 so that
        // leap day would show up each year
        DateOnly? date = new(year: 2024, month: 2, day: 29);
        DateOnly dateBefore = new(year: 2024, month: 2, day: 28);
        DateOnly dateAfter = new(year: 2024, month: 3, day: 1);

        bool result = TimeUtils.GetAnniversaryYearByRange(
            maybeDate: date,
            dateBefore: dateBefore,
            dateAfter: dateAfter,
            anniversaryYearCount: out uint anniversaryYearCount
            );

        Assert.AreEqual(
            expected: false,
            actual: result
            );
    }
}