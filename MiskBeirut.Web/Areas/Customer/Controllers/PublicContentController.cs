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
    protected const string DefaultLangCode = SiteLanguages.Default;

    private readonly PageContentManager _pages;
    private readonly ILanguageRepository _languages;
    private readonly SiteUrls _urls;

    protected PublicContentController(PageContentManager pages, ILanguageRepository languages, SiteUrls urls)
    {
        _pages = pages;
        _languages = languages;
        _urls = urls;
    }

    /// <summary>
    /// The language this request is being rendered in ("en" or "ar").
    ///
    /// The URL is what decides, not the cookie: /ar/about is the Arabic About page for everyone who
    /// opens that link, including a crawler and including someone whose last visit was in English.
    /// While language lived only in a cookie, both languages shared one URL, so there was no address
    /// to index for either and no way to link someone to the version you were reading. The cookie
    /// survives as a preference — it decides which language an unprefixed URL redirects into (see
    /// PublicUrlMiddleware) and nothing else.
    /// </summary>
    protected string CurrentLangCode => SiteLanguages.Normalize(
        RouteData.Values["lang"] as string ?? Request.Cookies[LangCookieName]);

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

        RememberLanguage(langCode);

        ViewData["Lang"] = langCode;
        ViewData["Culture"] = SiteLanguages.Culture(langCode);
        ViewData["Dir"] = content.IsRtl ? "rtl" : "ltr";
        ViewData["Title"] = SeoAttributes.ResolveTitle(content, page?.MetaTitle, pageName);
        ViewData["MetaDescription"] = SeoAttributes.ResolveDescription(content, page?.MetaDesc);
        ViewData["MetaKeywords"] = SeoAttributes.ResolveKeywords(content, page?.MetaKeyword);
        ViewData["Content"] = content;

        SetPageUrls(langCode, content);

        return content;
    }

    /// <summary>
    /// Canonical, hreflang alternates and the language switcher's destinations — all of them the
    /// same page in each language, so they are derived once from the current path with its language
    /// segment stripped off. A page is only ever reached through its own GET route, which is why
    /// Request.Path is a safe basis: it IS the indexable URL.
    /// </summary>
    private void SetPageUrls(string langCode, PageContent content)
    {
        var path = SiteUrls.PathWithoutLanguage(Request.Path);

        ViewData["CanonicalUrl"] = _urls.ForLanguage(Request, langCode, path);
        ViewData["AlternateUrls"] = SiteLanguages.All.ToDictionary(
            code => code,
            code => _urls.ForLanguage(Request, code, path));

        var shareImage = ResolveShareImage(content);
        ViewData["OgImage"] = shareImage is null ? null
            : shareImage.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? shareImage
            : _urls.Absolute(Request, shareImage);
    }

    /// <summary>
    /// The image a share card uses: whatever the page nominates, else its hero, else the site logo.
    /// An image already hosted elsewhere keeps its own absolute URL -- re-hosting it under our domain
    /// would just break it -- so making the URL absolute is left to the caller.
    /// </summary>
    private static string? ResolveShareImage(PageContent content)
        => new[] { content.Text("og_image"), content.Image("hero_image"), content.Global("logo_image") }
            .FirstOrDefault(candidate => !string.IsNullOrWhiteSpace(candidate));

    /// <summary>
    /// Records the language the visitor is actually reading, so that an unprefixed URL they arrive
    /// at later — an old bookmark, a link from before this change — redirects into that language
    /// instead of dropping them back into English.
    /// </summary>
    private void RememberLanguage(string langCode)
    {
        if (Request.Cookies[LangCookieName] == langCode)
            return;

        Response.Cookies.Append(LangCookieName, langCode, new CookieOptions
        {
            Path = "/",
            MaxAge = TimeSpan.FromDays(365),
            HttpOnly = false,
            IsEssential = true,
            SameSite = SameSiteMode.Lax
        });
    }
}
