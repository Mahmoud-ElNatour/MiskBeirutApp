using Microsoft.Extensions.Logging;
using MiskBeirut.Application.Dtos.Pages;
using MiskBeirut.Application.Emails;
using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Repositories;

namespace MiskBeirut.Application.Managers;

/// <summary>CMS page content: pages and their per-language attributes.</summary>
public class PageContentManager
{
    private const string GlobalPageName = "Global";

    /// <summary>Transactional emails are always written in English, regardless of the recipient's site language preference.</summary>
    private const string EmailLanguageCode = "en";

    private readonly IPageRepository _pages;
    private readonly ILanguageRepository _languages;
    private readonly ILogger<PageContentManager> _logger;

    public PageContentManager(IPageRepository pages, ILanguageRepository languages, ILogger<PageContentManager> logger)
    {
        _pages = pages;
        _languages = languages;
        _logger = logger;
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

    /// <summary>All pages (no attributes loaded) — for the Cms area's page list.</summary>
    public async Task<IReadOnlyList<PageDto>> GetAllPagesAsync(CancellationToken cancellationToken = default)
    {
        var pages = await _pages.GetAllAsync(cancellationToken);
        return pages.Select(ToDto).ToList();
    }

    /// <summary>Updates a page's SEO meta fields (its attributes are edited separately via SetAttributeAsync).</summary>
    public async Task<PageDto> UpdatePageMetaAsync(int id, string? metaTitle, string? metaDesc, string? metaKeyword, CancellationToken cancellationToken = default)
    {
        var page = await _pages.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Page {id} was not found.");

        page.MetaTitle = metaTitle;
        page.MetaDesc = metaDesc;
        page.MetaKeyword = metaKeyword;
        await _pages.UpdateAsync(page, cancellationToken);

        return ToDto(page);
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

    /// <summary>
    /// The footer contact details shown at the bottom of every transactional email, sourced from the
    /// same CMS-managed "Global" page attributes the site footer reads ("footer_phone"/"footer_email").
    /// Never throws: a lookup failure (CMS values unset, DB unavailable) is logged and falls back to
    /// <see cref="EmailFooterContact.Default"/> so a footer content issue never blocks an email send.
    /// </summary>
    public async Task<EmailFooterContact> GetEmailFooterContactAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var language = await _languages.GetByCodeAsync(EmailLanguageCode, cancellationToken);
            var global = await _pages.GetByNameAsync(GlobalPageName, cancellationToken);
            if (language is null || global is null)
                return EmailFooterContact.Default;

            string Get(string attributeName, string fallback)
            {
                var value = global.Attributes.FirstOrDefault(a => a.AttributeName == attributeName && a.LangId == language.Id)?.Value;
                return string.IsNullOrWhiteSpace(value) ? fallback : value;
            }

            return new EmailFooterContact(
                Get("footer_phone", EmailFooterContact.Default.Phone),
                Get("footer_email", EmailFooterContact.Default.Email));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load CMS footer content for outgoing email; using defaults.");
            return EmailFooterContact.Default;
        }
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
