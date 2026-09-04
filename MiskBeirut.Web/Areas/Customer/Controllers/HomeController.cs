using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MiskBeirut.Application.Managers;
using MiskBeirut.Core.Repositories;
using MiskBeirut.Web.Models;
using MiskBeirut.Web.Support;

namespace MiskBeirut.Web.Areas.Customer.Controllers;

public class HomeController : PublicContentController
{
    private readonly ILogger<HomeController> _logger;
    private readonly GoogleReviewManager _reviews;

    public HomeController(ILogger<HomeController> logger, PageContentManager pages, ILanguageRepository languages, SiteUrls urls, GoogleReviewManager reviews)
        : base(pages, languages, urls)
    {
        _logger = logger;
        _reviews = reviews;
    }

    public async Task<IActionResult> Index()
    {
        var content = await LoadPageAsync("Home");
        ViewData["GoogleReviews"] = await _reviews.GetFeaturedAsync(3);
        return View(content);
    }

    /// <summary>
    /// The public 404, re-executed by UseStatusCodePagesWithReExecute so the response keeps its 404
    /// status while rendering a real page. A redirect to an error page would answer 302 then 200,
    /// which tells a crawler the broken URL is fine and leaves it in the index — the exact opposite
    /// of what a 404 is for.
    /// </summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> PageNotFound()
    {
        var reExecute = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();

        // UseStatusCodePages re-executes for every empty-bodied 4xx and 5xx, not only 404 — an
        // antiforgery rejection on one of the public forms arrives here as a 400. Rendering "page
        // not found" over it would be wrong twice: the visitor is told the URL is broken when their
        // token expired, and the response advertises a status the request never had. Anything that
        // isn't a 404 keeps its own status and its empty body, exactly as before.
        var originalStatus = reExecute is null ? StatusCodes.Status404NotFound : Response.StatusCode;
        if (originalStatus != StatusCodes.Status404NotFound)
            return new EmptyResult();

        // Re-execution also discards the original request's route values, so the language has to be
        // recovered from the path that was actually requested — /ar/nope is a missing Arabic page,
        // and answering it in English because the visitor happens to have no cookie yet is its own
        // small bug. Putting the result back into RouteData is what keeps this page's nav and footer
        // links prefixed.
        RouteData.Values["lang"] = LanguageOf(reExecute?.OriginalPath) ?? CurrentLangCode;

        var content = await LoadPageAsync("Global");

        ViewData["Title"] = content.Global("not_found_title", "Page Not Found | Misk Beirut", "الصفحة غير موجودة | مسك بيروت");
        ViewData["MetaDescription"] = null;
        ViewData["NoIndex"] = true;

        // A URL that shouldn't exist has no canonical form and no translated counterpart; emitting
        // either would nominate this page for indexing under some other address.
        ViewData["CanonicalUrl"] = null;
        ViewData["AlternateUrls"] = null;

        // Set explicitly for the case reExecute is null: someone reaching this action by typing its
        // URL should still be told 404 rather than 200. On the re-executed path the middleware has
        // already put the original code here and this just restores the same value.
        Response.StatusCode = StatusCodes.Status404NotFound;
        return View(content);
    }

    /// <summary>The language segment of a path like "/ar/nope", or null if it has none.</summary>
    private static string? LanguageOf(string? path)
    {
        var first = (path ?? "").Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return SiteLanguages.IsSupported(first) ? first!.ToLowerInvariant() : null;
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
