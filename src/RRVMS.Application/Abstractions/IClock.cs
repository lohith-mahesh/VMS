namespace RRVMS.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
    DateOnly TodayInIndia { get; }
    TimeZoneInfo IndiaTimeZone { get; }
}

