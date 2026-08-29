using Microsoft.EntityFrameworkCore;
using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Repositories;
using MiskBeirut.Infrastructure.DbContexts;

namespace MiskBeirut.Infrastructure.Repositories;

public class BackofficePageRepository : Repository<BackofficePage>, IBackofficePageRepository
{
    public BackofficePageRepository(MiskBeirutDbContext context) : base(context)
    {
    }

    public Task<BackofficePage?> GetByNameAsync(string pageName, CancellationToken cancellationToken = default)
        => Context.BackofficePages
            .Include(p => p.Attributes)
            .FirstOrDefaultAsync(p => p.PageName == pageName, cancellationToken);

    public Task<BackofficePage?> GetWithAttributesAsync(int id, CancellationToken cancellationToken = default)
        => Context.BackofficePages
            .Include(p => p.Attributes)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<BackofficePageAttribute?> GetAttributeAsync(int pageId, string attributeName, CancellationToken cancellationToken = default)
        => Context.BackofficePageAttributes.FirstOrDefaultAsync(
            a => a.PageId == pageId && a.AttributeName == attributeName,
            cancellationToken);

    public async Task<BackofficePageAttribute> AddAttributeAsync(BackofficePageAttribute attribute, CancellationToken cancellationToken = default)
    {
        Context.BackofficePageAttributes.Add(attribute);
        await Context.SaveChangesAsync(cancellationToken);
        return attribute;
    }

    public async Task UpdateAttributeAsync(BackofficePageAttribute attribute, CancellationToken cancellationToken = default)
    {
        Context.BackofficePageAttributes.Update(attribute);
        await Context.SaveChangesAsync(cancellationToken);
    }
}
