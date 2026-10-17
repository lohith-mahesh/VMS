namespace RRVMS.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
