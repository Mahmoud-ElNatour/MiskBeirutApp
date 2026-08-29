namespace MiskBeirut.Core.Repositories;

/// <summary>
/// Wraps a block of repository calls in one database transaction. All repositories share a single
/// scoped DbContext, so nested repository/manager calls made inside <paramref name="operation"/>
/// automatically join the same transaction — nothing commits until <paramref name="operation"/>
/// returns, and everything rolls back if it throws.
/// </summary>
public interface IUnitOfWork
{
    Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default);
}
