namespace MiskBeirut.Web.Support;

/// <summary>
/// The languages the public site is published in, and the vocabulary the rest of the SEO plumbing
/// shares: the URL prefix ("/en/about", "/ar/about"), the <c>hreflang</c> value and the Open Graph
/// locale are all derived from the same list, so adding a third language is one entry here rather
/// than a hunt through the layout, the sitemap and the router.
/// </summary>
public static class SiteLanguages
{
    /// <summary>The language an unprefixed URL falls back to, and the <c>x-default</c> hreflang target.</summary>
    public const string Default = "en";

    /// <summary>Every published language code, in the order the sitemap and hreflang blocks list them.</summary>
    public static readonly string[] All = ["en", "ar"];


    public static bool IsSupported(string? code)
        => code is not null && All.Contains(code, StringComparer.OrdinalIgnoreCase);

    /// <summary><paramref name="code"/> when it is a language the site publishes, otherwise <see cref="Default"/>.</summary>
    public static string Normalize(string? code)
        => IsSupported(code) ? code!.ToLowerInvariant() : Default;

    public static bool IsRtl(string code) => code == "ar";

    /// <summary>The BCP 47 tag for &lt;html lang&gt; and og:locale (which wants the underscore form).</summary>
    public static string Culture(string code) => code == "ar" ? "ar-LB" : "en-US";

    public static string OpenGraphLocale(string code) => Culture(code).Replace('-', '_');
}
