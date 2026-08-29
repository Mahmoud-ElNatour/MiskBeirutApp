using MiskBeirut.Core.Repositories;
using MiskBeirut.Infrastructure.DbContexts;

namespace MiskBeirut.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IUnitOfWork"/> — a plain BeginTransaction/Commit/Rollback
/// around the shared scoped <see cref="MiskBeirutDbContext"/>.</summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly MiskBeirutDbContext _context;

    public UnitOfWork(MiskBeirutDbContext context)
    {
        _context = context;
    }

    public async Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await operation();
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
