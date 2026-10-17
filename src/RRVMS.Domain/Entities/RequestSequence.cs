namespace RRVMS.Domain.Entities;

public sealed class RequestSequence
{
    private RequestSequence()
    {
    }

    public RequestSequence(int year, int month)
    {
        Year = year;
        Month = month;
    }

    public int Year { get; private set; }
    public int Month { get; private set; }
    public int CurrentValue { get; private set; }

    public int Next()
    {
        CurrentValue++;
        return CurrentValue;
    }
}

