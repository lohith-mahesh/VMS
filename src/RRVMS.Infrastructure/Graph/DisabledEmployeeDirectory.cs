using RRVMS.Application.Abstractions;

namespace RRVMS.Infrastructure.Graph;

public sealed class DisabledEmployeeDirectory : IEmployeeDirectory
{
    public Task<IReadOnlyList<EmployeeDirectoryResult>> SearchAsync(string query, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<EmployeeDirectoryResult>>([]);

    public Task<EmployeeDirectoryResult?> FindByIdAsync(string objectId, CancellationToken cancellationToken) =>
        Task.FromResult<EmployeeDirectoryResult?>(null);
}
