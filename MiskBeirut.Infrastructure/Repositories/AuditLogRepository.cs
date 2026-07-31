using Microsoft.EntityFrameworkCore;
using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Repositories;
using MiskBeirut.Infrastructure.DbContexts;

namespace MiskBeirut.Infrastructure.Repositories;

public class AuditLogRepository : Repository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(MiskBeirutDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<AuditLog>> GetRecentAsync(int count, CancellationToken cancellationToken = default)
        => await Context.AuditLogs
            .AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
}
