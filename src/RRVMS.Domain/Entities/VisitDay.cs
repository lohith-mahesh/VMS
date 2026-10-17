using RRVMS.Domain.Common;

namespace RRVMS.Domain.Entities;

public sealed class VisitDay : Entity
{
    private VisitDay()
    {
    }

    internal VisitDay(Guid requestId, DateOnly date, TimeOnly expectedArrival, TimeOnly expectedDeparture)
    {
        RequestId = requestId;
        Date = date;
        ExpectedArrival = expectedArrival;
        ExpectedDeparture = expectedDeparture;
    }

    public Guid RequestId { get; private set; }
    public DateOnly Date { get; private set; }
    public TimeOnly ExpectedArrival { get; private set; }
    public TimeOnly ExpectedDeparture { get; private set; }
}

