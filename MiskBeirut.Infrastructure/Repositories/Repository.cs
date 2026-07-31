using Microsoft.EntityFrameworkCore;
using MiskBeirut.Core.Repositories;
using MiskBeirut.Infrastructure.DbContexts;

namespace MiskBeirut.Infrastructure.Repositories;

/// <summary>EF Core implementation of the shared CRUD contract.</summary>
public abstract class Repository<T> : IRepository<T> where T : class
{
    protected readonly MiskBeirutDbContext Context;

    protected Repository(MiskBeirutDbContext context)
    {
        Context = context;
    }

    public virtual async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => await Context.Set<T>().FindAsync(new object[] { id }, cancellationToken);

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
        => await Context.Set<T>().AsNoTracking().ToListAsync(cancellationToken);

    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        Context.Set<T>().Add(entity);
        await Context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public virtual async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        Context.Set<T>().Update(entity);
        await Context.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        Context.Set<T>().Remove(entity);
        await Context.SaveChangesAsync(cancellationToken);
    }
}
