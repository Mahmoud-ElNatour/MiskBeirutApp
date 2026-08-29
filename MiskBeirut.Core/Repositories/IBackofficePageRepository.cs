using MiskBeirut.Core.Entities;

namespace MiskBeirut.Core.Repositories;

/// <summary>Backoffice (Admin-area) CMS pages and their attributes.</summary>
public interface IBackofficePageRepository : IRepository<BackofficePage>
{
    Task<BackofficePage?> GetByNameAsync(string pageName, CancellationToken cancellationToken = default);
    Task<BackofficePage?> GetWithAttributesAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Looks up an attribute by its natural key (PageId, AttributeName).</summary>
    Task<BackofficePageAttribute?> GetAttributeAsync(int pageId, string attributeName, CancellationToken cancellationToken = default);
    Task<BackofficePageAttribute> AddAttributeAsync(BackofficePageAttribute attribute, CancellationToken cancellationToken = default);
    Task UpdateAttributeAsync(BackofficePageAttribute attribute, CancellationToken cancellationToken = default);
}
