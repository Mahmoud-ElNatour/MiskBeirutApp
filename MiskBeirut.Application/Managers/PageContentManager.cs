using MiskBeirut.Application.Dtos.Pages;
using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Repositories;

namespace MiskBeirut.Application.Managers;

/// <summary>CMS page content: pages and their per-language attributes.</summary>
public class PageContentManager
{
    private readonly IPageRepository _pages;

    public PageContentManager(IPageRepository pages)
    {
        _pages = pages;
    }

    public async Task<PageDto?> GetPageAsync(int id, CancellationToken cancellationToken = default)
    {
        var page = await _pages.GetWithAttributesAsync(id, cancellationToken);
        return page is null ? null : ToDto(page);
    }

    public async Task<PageDto?> GetPageByNameAsync(string pageName, CancellationToken cancellationToken = default)
    {
        var page = await _pages.GetByNameAsync(pageName, cancellationToken);
        return page is null ? null : ToDto(page);
    }

    /// <summary>
    /// Upserts an attribute. (PageId, AttributeName, LangId) is unique, so an existing
    /// row is updated in place instead of inserting and letting the unique constraint
    /// surface as an unhandled exception.
    /// </summary>
    public async Task<PageAttributeDto> SetAttributeAsync(SetPageAttributeRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _pages.GetAttributeAsync(request.PageId, request.AttributeName, request.LangId, cancellationToken);
        if (existing is not null)
        {
            existing.AttributeType = request.AttributeType;
            existing.Value = request.Value;
            await _pages.UpdateAttributeAsync(existing, cancellationToken);
            return ToDto(existing);
        }

        var attribute = await _pages.AddAttributeAsync(new PageAttribute
        {
            PageId = request.PageId,
            AttributeName = request.AttributeName,
            AttributeType = request.AttributeType,
            LangId = request.LangId,
            Value = request.Value
        }, cancellationToken);

        return ToDto(attribute);
    }

    private static PageDto ToDto(Page page) => new()
    {
        Id = page.Id,
        PageName = page.PageName,
        MetaTitle = page.MetaTitle,
        MetaDesc = page.MetaDesc,
        MetaKeyword = page.MetaKeyword,
        Attributes = page.Attributes.Select(ToDto).ToList()
    };

    private static PageAttributeDto ToDto(PageAttribute attribute) => new()
    {
        Id = attribute.Id,
        PageId = attribute.PageId,
        AttributeName = attribute.AttributeName,
        AttributeType = attribute.AttributeType,
        LangId = attribute.LangId,
        Value = attribute.Value
    };
}
