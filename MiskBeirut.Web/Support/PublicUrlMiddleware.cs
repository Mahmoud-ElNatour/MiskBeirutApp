namespace MiskBeirut.Web.Support;

/// <summary>
/// Makes every public page reachable at exactly one URL, and sends every other spelling of it there.
///
/// Three things get normalized, in one redirect rather than a chain of them:
///
/// * <b>Host.</b> www and non-www served the same pages from two hostnames. Whichever
///   Site:CanonicalHost names is the one that answers; the other 301s to it.
/// * <b>Case and trailing slash.</b> "/EN/About/" and "/en/about" are the same page to the router
///   and two different pages to a crawler.
/// * <b>Language prefix.</b> The site used to pick a language from a cookie and serve every
///   language at the same URL, so /about was English or Arabic depending on who asked and there was
///   no address for either version on its own. Unprefixed URLs now redirect into /en/... or /ar/...
///
/// Only the language step depends on who is asking (the cookie carries the choice made in the nav),
/// so only a redirect that adds a prefix is temporary. Host and spelling fixes are the same for
/// everyone and are permanent, which is what lets a crawler collapse the old URLs onto the new ones.
///
/// Runs after UseStaticFiles so wwwroot is untouched, and before UseRouting so the router only ever
/// sees a normalized path.
/// </summary>
public sealed class PublicUrlMiddleware
{
    private const string LangCookieName = "lang";

    /// <summary>
    /// Paths that must answer at the site root, with no language segment: crawlers fetch /robots.txt
    /// and /sitemap.xml by convention and will not follow a redirect to a language-prefixed copy.
    /// </summary>
    private static readonly string[] RootPaths = ["/robots.txt", "/sitemap.xml", "/favicon.ico"];

    /// <summary>
    /// Static roots. UseStaticFiles has already had its turn by the time this runs, so a request
    /// that reaches here under one of these is a miss — it should 404 as itself rather than be
    /// redirected into /en/img/... and 404 there, which would hide the real cause.
    /// </summary>
    private static readonly string[] AssetPrefixes = ["/css", "/js", "/img", "/lib", "/pdf", "/uploads"];

    private readonly RequestDelegate _next;
    private readonly string? _canonicalHost;

    public PublicUrlMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        var host = configuration["Site:CanonicalHost"];
        _canonicalHost = string.IsNullOrWhiteSpace(host) ? null : host.Trim().TrimEnd('/');
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;

        // A POST can't be redirected without losing its body — a 302 turns it into a GET. Every form
        // on the site posts to a URL this app generated, which already carries the prefix, so the
        // only requests that need fixing are the ones someone typed, bookmarked or linked.
        if (!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method))
        {
            await _next(context);
            return;
        }

        if (!IsPublicHost(request.Host) || IsExcluded(request.Path))
        {
            await _next(context);
            return;
        }

        var path = request.Path.Value ?? "/";
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        var hasLanguage = segments.Length > 0 && SiteLanguages.IsSupported(segments[0]);
        var langCode = hasLanguage
            ? segments[0].ToLowerInvariant()
            : SiteLanguages.Normalize(request.Cookies[LangCookieName]);

        var rest = hasLanguage ? segments[1..] : segments;
        var target = "/" + langCode;
        if (rest.Length > 0)
            target += "/" + string.Join('/', rest).ToLowerInvariant();

        if (target == path)
        {
            await _next(context);
            return;
        }

        var host = _canonicalHost is null ? request.Host.Value : _canonicalHost;
        var scheme = _canonicalHost is null ? request.Scheme : "https";

        // Adding a prefix is a per-visitor decision (the cookie), so it stays temporary; correcting
        // the host or the spelling of an already-prefixed URL is true for everyone and is permanent.
        context.Response.Redirect($"{scheme}://{host}{target}{request.QueryString}", permanent: hasLanguage);
    }

    /// <summary>
    /// The public site is everything that isn't one of the two back-office subdomains — the same
    /// split Program.cs's RequireHost routes make. Those areas are behind a login and are told not
    /// to be crawled at all (see SeoController.Robots), so rewriting their URLs would be noise.
    /// </summary>
    public static bool IsPublicHost(HostString host)
        => !host.Host.StartsWith("backoffice.", StringComparison.OrdinalIgnoreCase)
           && !host.Host.StartsWith("cms.", StringComparison.OrdinalIgnoreCase);

    private static bool IsExcluded(PathString path)
    {
        var value = path.Value ?? "/";

        if (RootPaths.Contains(value, StringComparer.OrdinalIgnoreCase))
            return true;

        if (AssetPrefixes.Any(prefix => value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            return true;

        // Anything that still looks like a file ("/apple-touch-icon.png", a stray ".well-known"
        // probe) — redirecting it into a language folder turns a clean 404 into a confusing one.
        var lastSegment = value[(value.LastIndexOf('/') + 1)..];
        return lastSegment.Contains('.');
    }
}
