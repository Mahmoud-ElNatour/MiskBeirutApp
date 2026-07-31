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

    private readonly PageContentManager _pages;
    private readonly ILanguageRepository _languages;

    protected PublicContentController(PageContentManager pages, ILanguageRepository languages)
    {
        _pages = pages;
        _languages = languages;
    }

    protected async Task<PageContent> LoadPageAsync(string pageName, CancellationToken cancellationToken = default)
    {
        var langCode = Request.Cookies[LangCookieName] == "ar" ? "ar" : "en";

        var language = await _languages.GetByCodeAsync(langCode, cancellationToken)
            ?? await _languages.GetByCodeAsync("en", cancellationToken);
        var langId = language?.Id ?? 1;

        var page = await _pages.GetPageByNameAsync(pageName, cancellationToken);
        var global = await _pages.GetPageByNameAsync(GlobalPageName, cancellationToken);

        var content = new PageContent(page, global, langId, langCode);

        ViewData["Lang"] = langCode;
        ViewData["Dir"] = content.IsRtl ? "rtl" : "ltr";
        ViewData["Title"] = page?.MetaTitle ?? $"{pageName} | Misk Beirut";
        ViewData["MetaDescription"] = page?.MetaDesc;
        ViewData["Content"] = content;

        return content;
    }
}
