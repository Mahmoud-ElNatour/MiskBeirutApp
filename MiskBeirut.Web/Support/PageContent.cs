using MiskBeirut.Application.Dtos.Pages;

namespace MiskBeirut.Web.Support;

/// <summary>
/// Read-only view over a page's CMS attributes (already filtered to the current language),
/// plus the shared "Global" page's attributes for nav/footer/brand content.
/// </summary>
public sealed class PageContent
{
    private readonly Dictionary<string, string?> _page;
    private readonly Dictionary<string, string?> _global;

    /// <summary>
    /// Same two dictionaries built for the site's default language. On the public site these back a
    /// per-attribute fallback: content an editor has only entered once (photos, map URLs, social
    /// links, a section a translator hasn't reached yet) shows on BOTH language versions instead of
    /// the Arabic page silently dropping to a hardcoded English default or an empty gallery tile.
    /// Left empty for the Cms preview, where "nothing set for this language" is exactly what the
    /// editor needs to see.
    /// </summary>
    private readonly Dictionary<string, string?> _pageFallback;
    private readonly Dictionary<string, string?> _globalFallback;

    public string LangCode { get; }
    public bool IsRtl => LangCode == "ar";

    /// <param name="fallbackLangId">
    /// Language to fall back to when an attribute has no value for <paramref name="langId"/>.
    /// Pass the same value as <paramref name="langId"/> (or 0) to disable the fallback.
    /// </param>
    public PageContent(PageDto? page, PageDto? global, int langId, string langCode, int fallbackLangId = 0)
    {
        LangCode = langCode;
        _page = ToDictionary(page, langId);
        _global = ToDictionary(global, langId);

        var useFallback = fallbackLangId != 0 && fallbackLangId != langId;
        _pageFallback = useFallback ? ToDictionary(page, fallbackLangId) : new Dictionary<string, string?>();
        _globalFallback = useFallback ? ToDictionary(global, fallbackLangId) : new Dictionary<string, string?>();
    }

    private static Dictionary<string, string?> ToDictionary(PageDto? page, int langId)
        => page?.Attributes
            .Where(a => a.LangId == langId)
            .ToDictionary(a => a.AttributeName, a => a.Value)
           ?? new Dictionary<string, string?>();

    private static bool TryGet(Dictionary<string, string?> primary, Dictionary<string, string?> fallback, string name, out string value)
    {
        if (primary.TryGetValue(name, out var v) && !string.IsNullOrEmpty(v))
        {
            value = v;
            return true;
        }

        if (fallback.TryGetValue(name, out var f) && !string.IsNullOrEmpty(f))
        {
            value = f;
            return true;
        }

        value = "";
        return false;
    }

    public string Text(string name, string fallback = "") => TryGet(_page, _pageFallback, name, out var v) ? v : fallback;

    public string Image(string name, string fallback = "") => Text(name, fallback);

    public string Link(string name, string fallback = "#") => Text(name, fallback);

    /// <summary>
    /// The src for an embedded Google map. Values entered through the Cms are already normalized on
    /// save (see MapEmbedUrl), but rows predate that, so whatever is stored is converted again here
    /// — a link that only ever worked as a page link would otherwise leave a blank rectangle.
    /// </summary>
    /// <param name="addressLinkName">
    /// The "open in Google Maps" link on the same page. When no embed URL of its own has been set,
    /// the map follows that link, so an editor who updates the address in one place gets a map that
    /// agrees with it instead of one still pointing at the old location.
    /// </param>
    public string MapEmbed(string name, string addressLinkName, string fallback)
    {
        if (TryGet(_page, _pageFallback, name, out var configured) &&
            MapEmbedUrl.Normalize(configured) is { Length: > 0 } normalized)
            return normalized;

        if (TryGet(_page, _pageFallback, addressLinkName, out var addressLink) &&
            MapEmbedUrl.Normalize(addressLink) is { Length: > 0 } derived)
            return derived;

        return fallback;
    }

    public bool Has(string name) => TryGet(_page, _pageFallback, name, out _);

    public string Global(string name, string fallback = "") => TryGet(_global, _globalFallback, name, out var v) ? v : fallback;

    /// <summary>
    /// Language-aware default for copy that lives in the view rather than the CMS — chiefly the
    /// client-side validation and status messages, which have no attribute row to read from. Returns
    /// the CMS value if the editor has set one, otherwise the Arabic or English default per the
    /// current language, so an Arabic visitor never sees an English error string.
    /// </summary>
    public string Text(string name, string englishDefault, string arabicDefault)
        => Text(name, IsRtl ? arabicDefault : englishDefault);

    /// <inheritdoc cref="Text(string,string,string)"/>
    public string Global(string name, string englishDefault, string arabicDefault)
        => Global(name, IsRtl ? arabicDefault : englishDefault);

    /// <summary>
    /// Highest numeric index found among attributes named "{prefix}{N}{suffix}" — e.g.
    /// MaxIndex("football_gallery_", "_image") matches "football_gallery_7_image" and returns 7.
    /// Lets an open-ended "gallery" of numbered fields (see Areas/Customer/Views/Events/Index.cshtml)
    /// render exactly as many slots as actually exist, with no hardcoded cap: the CMS's "Add Photo"
    /// button picks the next index client-side, so this is how the page discovers, on its next load,
    /// how far that numbering has grown.
    /// </summary>
    public int MaxIndex(string prefix, string suffix)
    {
        var max = 0;
        foreach (var key in _page.Keys.Concat(_pageFallback.Keys))
        {
            if (key.Length <= prefix.Length + suffix.Length) continue;
            if (!key.StartsWith(prefix, StringComparison.Ordinal) || !key.EndsWith(suffix, StringComparison.Ordinal)) continue;
            var middle = key.Substring(prefix.Length, key.Length - prefix.Length - suffix.Length);
            if (int.TryParse(middle, out var n) && n > max) max = n;
        }
        return max;
    }
}
