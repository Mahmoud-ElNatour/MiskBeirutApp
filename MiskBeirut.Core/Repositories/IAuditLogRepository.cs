using MiskBeirut.Core.Entities;

namespace MiskBeirut.Core.Repositories;

public interface IAuditLogRepository : IRepository<AuditLog>
{
    Task<IReadOnlyList<AuditLog>> GetRecentAsync(int count, CancellationToken cancellationToken = default);

    /// <summary>Every log entry for a given month/year (either or both may be null, meaning "any"), newest first — used by the Audit Logs page's month filter, unlike <see cref="GetRecentAsync"/>'s fixed row cap.</summary>
    Task<IReadOnlyList<AuditLog>> GetByMonthAsync(int? month, int? year, CancellationToken cancellationToken = default);
}
