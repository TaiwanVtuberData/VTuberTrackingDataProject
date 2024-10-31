using LanguageExt;

namespace GenerateAdvertisement.Types;

public class TimeInterval
{
    DateTimeOffset startTime;
    DateTimeOffset endTime;

    TimeInterval(DateTimeOffset startTime, DateTimeOffset endTime)
    {
        Validation<ValidationError, TimeInterval> validationResult = Validate(startTime, endTime);

        validationResult.Match(
            result =>
            {
                this.startTime = result.startTime;
                this.endTime = result.endTime;
            },
            errors =>
            {
                throw new ArgumentException(errors.Aggregate("", (a, b) => a + "\n" + b));
            }
        );
    }

    public bool Within(DateTimeOffset time)
    {
        return startTime < time && time < endTime;
    }

    public static Validation<ValidationError, TimeInterval> Validate(
        DateTimeOffset startTime,
        DateTimeOffset endTime
    )
    {
        if (startTime > endTime)
        {
            return new ValidationError(
                $"Invalid time interval from [${startTime}] to [${endTime}]."
            );
        }

        return new TimeInterval(startTime, endTime, dummy: true);
    }

    private TimeInterval(DateTimeOffset startTime, DateTimeOffset endTime, bool dummy)
    {
        this.startTime = startTime;
        this.endTime = endTime;
    }
}
