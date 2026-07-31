using MiskBeirut.Core.Entities;

namespace MiskBeirut.Core.Repositories;

public interface IAuditLogRepository : IRepository<AuditLog>
{
    Task<IReadOnlyList<AuditLog>> GetRecentAsync(int count, CancellationToken cancellationToken = default);
}
