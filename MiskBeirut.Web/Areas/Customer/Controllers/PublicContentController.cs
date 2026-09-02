using Microsoft.AspNetCore.Mvc;
using MiskBeirut.Application.Managers;
using MiskBeirut.Core.Repositories;
using MiskBeirut.Web.Support;

namespace MiskBeirut.Web.Areas.Customer.Controllers;

/// <summary>Base for public marketing pages: resolves the current language and loads CMS content.</summary>
[Area("Customer")]
public abstract class PublicContentController : Controller
{
    private const string GlobalPageName = "Global";
    private const string LangCookieName = "lang";

    /// <summary>The language every other one falls back to when an attribute has no translation yet.</summary>
    protected const string DefaultLangCode = "en";

    private readonly PageContentManager _pages;
    private readonly ILanguageRepository _languages;

    protected PublicContentController(PageContentManager pages, ILanguageRepository languages)
    {
        _pages = pages;
        _languages = languages;
    }

    /// <summary>The language this request is being rendered in ("en" or "ar").</summary>
    protected string CurrentLangCode => Request.Cookies[LangCookieName] == "ar" ? "ar" : DefaultLangCode;

    protected async Task<PageContent> LoadPageAsync(string pageName, CancellationToken cancellationToken = default)
    {
        var langCode = CurrentLangCode;

        var language = await _languages.GetByCodeAsync(langCode, cancellationToken)
            ?? await _languages.GetByCodeAsync(DefaultLangCode, cancellationToken);
        var langId = language?.Id ?? 1;

        var defaultLanguage = langCode == DefaultLangCode
            ? language
            : await _languages.GetByCodeAsync(DefaultLangCode, cancellationToken);
        var defaultLangId = defaultLanguage?.Id ?? 1;

        var page = await _pages.GetPageByNameAsync(pageName, cancellationToken);
        var global = await _pages.GetPageByNameAsync(GlobalPageName, cancellationToken);

        var content = new PageContent(page, global, langId, langCode, defaultLangId);

        ViewData["Lang"] = langCode;
        ViewData["Dir"] = content.IsRtl ? "rtl" : "ltr";
        ViewData["Title"] = SeoAttributes.ResolveTitle(content, page?.MetaTitle, pageName);
        ViewData["MetaDescription"] = SeoAttributes.ResolveDescription(content, page?.MetaDesc);
        ViewData["MetaKeywords"] = SeoAttributes.ResolveKeywords(content, page?.MetaKeyword);
        ViewData["Content"] = content;

        return content;
    }
}
