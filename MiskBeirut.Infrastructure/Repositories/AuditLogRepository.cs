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

    public async Task<IReadOnlyList<AuditLog>> GetByMonthAsync(int? month, int? year, CancellationToken cancellationToken = default)
    {
        var query = Context.AuditLogs.AsNoTracking().AsQueryable();

        if (month.HasValue)
            query = query.Where(a => a.CreatedAt.Month == month.Value);
        if (year.HasValue)
            query = query.Where(a => a.CreatedAt.Year == year.Value);

        return await query.OrderByDescending(a => a.CreatedAt).ToListAsync(cancellationToken);
    }
}
