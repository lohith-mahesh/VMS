namespace RRVMS.Application.Abstractions;

public sealed record EmployeeDirectoryResult(string ObjectId, string DisplayName, string Email, string Department);

public interface IEmployeeDirectory
{
    Task<IReadOnlyList<EmployeeDirectoryResult>> SearchAsync(string query, CancellationToken cancellationToken);
    Task<EmployeeDirectoryResult?> FindByIdAsync(string objectId, CancellationToken cancellationToken);
}
