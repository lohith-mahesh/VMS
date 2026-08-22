namespace RRVMS.Api.Services;

public interface ICurrentUserService
{
    string UserId { get; }
    string Name { get; }
    string Email { get; }
    string Role { get; }
    bool IsAuthenticated { get; }
}
