using MiskBeirut.Core.Entities;

namespace MiskBeirut.Core.Repositories;

/// <summary>CMS pages and their attributes.</summary>
public interface IPageRepository : IRepository<Page>
{
    Task<Page?> GetByNameAsync(string pageName, CancellationToken cancellationToken = default);
    Task<Page?> GetWithAttributesAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Looks up an attribute by its natural key (PageId, AttributeName, LangId).</summary>
    Task<PageAttribute?> GetAttributeAsync(int pageId, string attributeName, int langId, CancellationToken cancellationToken = default);
    Task<PageAttribute> AddAttributeAsync(PageAttribute attribute, CancellationToken cancellationToken = default);
    Task UpdateAttributeAsync(PageAttribute attribute, CancellationToken cancellationToken = default);
}
