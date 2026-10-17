using RRVMS.Domain.Common;

namespace RRVMS.Domain.ValueObjects;

public sealed record VisitWindow
{
    private VisitWindow(DateTimeOffset start, DateTimeOffset end)
    {
        Start = start;
        End = end;
    }

    public DateTimeOffset Start { get; }
    public DateTimeOffset End { get; }

    public static VisitWindow Create(DateTimeOffset start, DateTimeOffset end, DateTimeOffset now)
    {
        if (start <= now)
        {
            throw new DomainException("The visit start must be in the future.");
        }

        if (end <= start)
        {
            throw new DomainException("The visit end must be after the visit start.");
        }

        if (end - start > TimeSpan.FromDays(365))
        {
            throw new DomainException("A visit cannot span more than 365 days.");
        }

        return new VisitWindow(start, end);
    }

    public IEnumerable<(DateOnly Date, TimeOnly Arrival, TimeOnly Departure)> Expand()
    {
        var startDate = DateOnly.FromDateTime(Start.Date);
        var endDate = DateOnly.FromDateTime(End.Date);

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var arrival = date == startDate ? TimeOnly.FromDateTime(Start.DateTime) : TimeOnly.MinValue;
            var departure = date == endDate ? TimeOnly.FromDateTime(End.DateTime) : new TimeOnly(23, 59);
            yield return (date, arrival, departure);
        }
    }
}

