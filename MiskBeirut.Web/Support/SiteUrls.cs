namespace MiskBeirut.Web.Support;

/// <summary>
/// Builds the absolute URLs the public site has to state about itself — canonical, hreflang
/// alternates, Open Graph and the sitemap.
///
/// These cannot be generated from the request host the way an ordinary link can. The same page is
/// reachable as miskbeirut.com, www.miskbeirut.com and (on the deployed box) the raw IIS binding,
/// and a canonical tag that simply echoes whichever host the visitor happened to use tells a
/// crawler that all of them are separate, equally-authoritative pages — which is the duplicate
/// content the tag exists to prevent. So the host is configuration (Site:CanonicalHost), and only
/// falls back to the request when it isn't set, which is the local-development case.
/// </summary>
public sealed class SiteUrls
{
    private readonly string? _canonicalHost;

    public SiteUrls(IConfiguration configuration)
    {
        var host = configuration["Site:CanonicalHost"];
        _canonicalHost = string.IsNullOrWhiteSpace(host) ? null : host.Trim().TrimEnd('/');
    }

    /// <summary>"https://miskbeirut.com" — no trailing slash.</summary>
    public string BaseUrl(HttpRequest request)
        => _canonicalHost is null
            ? $"{request.Scheme}://{request.Host.Value}"
            : $"https://{_canonicalHost}";

    /// <summary>
    /// The absolute form of a site-relative path ("/en/about" -> "https://miskbeirut.com/en/about").
    /// Left exactly as given: this also carries asset paths (og:image), where changing the case of a
    /// filename is not a normalization but a broken link.
    /// </summary>
    public string Absolute(HttpRequest request, string path)
        => BaseUrl(request) + (path.StartsWith('/') ? path : "/" + path);

    /// <summary>
    /// The current page's path with its language segment removed: "/en/about" -> "/about", "/ar" -> "".
    /// This is the part that is identical across languages, so it is what the alternates are built from.
    /// </summary>
    public static string PathWithoutLanguage(PathString path)
    {
        var value = path.Value ?? "";
        var segments = value.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length > 0 && SiteLanguages.IsSupported(segments[0]))
            segments = segments[1..];

        return segments.Length == 0 ? "" : "/" + string.Join('/', segments);
    }

    /// <summary>
    /// The absolute URL of <paramref name="pathWithoutLanguage"/> in <paramref name="langCode"/>.
    /// Both the canonical tag (this language) and every hreflang alternate (each language in turn)
    /// come through here, so a canonical can never disagree with the alternate that points at it.
    /// </summary>
    public string ForLanguage(HttpRequest request, string langCode, string pathWithoutLanguage)
        => BaseUrl(request) + $"/{SiteLanguages.Normalize(langCode)}{NormalizePagePath(pathWithoutLanguage)}";

    /// <summary>
    /// Lower-cased, no trailing slash — the single spelling of a URL that the canonical tag, the
    /// sitemap and PublicUrlMiddleware's redirects all agree on. "/" is left as "" so a base URL is
    /// never emitted with a bare trailing slash on one page and without it on another.
    /// </summary>
    private static string NormalizePagePath(string path)
    {
        if (string.IsNullOrEmpty(path) || path == "/")
            return "";

        if (!path.StartsWith('/'))
            path = "/" + path;

        return path.TrimEnd('/').ToLowerInvariant();
    }
}
