using MiskBeirut.Application.Dtos.Pages;
using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Repositories;

namespace MiskBeirut.Application.Managers;

/// <summary>Backoffice (Admin-area) CMS page content: pages and their attributes.</summary>
public class BackofficePageContentManager
{
    private readonly IBackofficePageRepository _pages;

    public BackofficePageContentManager(IBackofficePageRepository pages)
    {
        _pages = pages;
    }

    public async Task<BackofficePageDto?> GetPageAsync(int id, CancellationToken cancellationToken = default)
    {
        var page = await _pages.GetWithAttributesAsync(id, cancellationToken);
        return page is null ? null : ToDto(page);
    }

    public async Task<BackofficePageDto?> GetPageByNameAsync(string pageName, CancellationToken cancellationToken = default)
    {
        var page = await _pages.GetByNameAsync(pageName, cancellationToken);
        return page is null ? null : ToDto(page);
    }

    /// <summary>
    /// Upserts an attribute. (PageId, AttributeName) is unique, so an existing row is updated
    /// in place instead of inserting and letting the unique constraint surface as an unhandled
    /// exception.
    /// </summary>
    public async Task<BackofficePageAttributeDto> SetAttributeAsync(SetBackofficePageAttributeRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _pages.GetAttributeAsync(request.PageId, request.AttributeName, cancellationToken);
        if (existing is not null)
        {
            existing.AttributeType = request.AttributeType;
            existing.Value = request.Value;
            await _pages.UpdateAttributeAsync(existing, cancellationToken);
            return ToDto(existing);
        }

        var attribute = await _pages.AddAttributeAsync(new BackofficePageAttribute
        {
            PageId = request.PageId,
            AttributeName = request.AttributeName,
            AttributeType = request.AttributeType,
            Value = request.Value
        }, cancellationToken);

        return ToDto(attribute);
    }

    private static BackofficePageDto ToDto(BackofficePage page) => new()
    {
        Id = page.Id,
        PageName = page.PageName,
        Attributes = page.Attributes.Select(ToDto).ToList()
    };

    private static BackofficePageAttributeDto ToDto(BackofficePageAttribute attribute) => new()
    {
        Id = attribute.Id,
        PageId = attribute.PageId,
        AttributeName = attribute.AttributeName,
        AttributeType = attribute.AttributeType,
        Value = attribute.Value
    };
}
