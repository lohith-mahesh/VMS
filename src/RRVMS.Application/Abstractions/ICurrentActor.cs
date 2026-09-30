using RRVMS.Domain.Enums;

namespace RRVMS.Application.Abstractions;

public interface ICurrentActor
{
    string ObjectId { get; }
    string DisplayName { get; }
    string Email { get; }
    UserRole Role { get; }
    string CorrelationId { get; }
    bool IsAuthenticated { get; }
}

