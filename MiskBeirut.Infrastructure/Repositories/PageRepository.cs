using Microsoft.EntityFrameworkCore;
using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Repositories;
using MiskBeirut.Infrastructure.DbContexts;

namespace MiskBeirut.Infrastructure.Repositories;

public class PageRepository : Repository<Page>, IPageRepository
{
    public PageRepository(MiskBeirutDbContext context) : base(context)
    {
    }

    public Task<Page?> GetByNameAsync(string pageName, CancellationToken cancellationToken = default)
        => Context.Pages
            .Include(p => p.Attributes)
            .FirstOrDefaultAsync(p => p.PageName == pageName, cancellationToken);

    public Task<Page?> GetWithAttributesAsync(int id, CancellationToken cancellationToken = default)
        => Context.Pages
            .Include(p => p.Attributes)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<PageAttribute?> GetAttributeAsync(int pageId, string attributeName, int langId, CancellationToken cancellationToken = default)
        => Context.PageAttributes.FirstOrDefaultAsync(
            a => a.PageId == pageId && a.AttributeName == attributeName && a.LangId == langId,
            cancellationToken);

    public async Task<PageAttribute> AddAttributeAsync(PageAttribute attribute, CancellationToken cancellationToken = default)
    {
        Context.PageAttributes.Add(attribute);
        await Context.SaveChangesAsync(cancellationToken);
        return attribute;
    }

    public async Task UpdateAttributeAsync(PageAttribute attribute, CancellationToken cancellationToken = default)
    {
        Context.PageAttributes.Update(attribute);
        await Context.SaveChangesAsync(cancellationToken);
    }
}
