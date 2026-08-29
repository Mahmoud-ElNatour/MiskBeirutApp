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

    public string LangCode { get; }
    public bool IsRtl => LangCode == "ar";

    public PageContent(PageDto? page, PageDto? global, int langId, string langCode)
    {
        LangCode = langCode;
        _page = ToDictionary(page, langId);
        _global = ToDictionary(global, langId);
    }

    private static Dictionary<string, string?> ToDictionary(PageDto? page, int langId)
        => page?.Attributes
            .Where(a => a.LangId == langId)
            .ToDictionary(a => a.AttributeName, a => a.Value)
           ?? new Dictionary<string, string?>();

    public string Text(string name, string fallback = "") => _page.TryGetValue(name, out var v) && !string.IsNullOrEmpty(v) ? v! : fallback;

    public string Image(string name, string fallback = "") => Text(name, fallback);

    public string Link(string name, string fallback = "#") => Text(name, fallback);

    public bool Has(string name) => _page.TryGetValue(name, out var v) && !string.IsNullOrEmpty(v);

    public string Global(string name, string fallback = "") => _global.TryGetValue(name, out var v) && !string.IsNullOrEmpty(v) ? v! : fallback;

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
        foreach (var key in _page.Keys)
        {
            if (key.Length <= prefix.Length + suffix.Length) continue;
            if (!key.StartsWith(prefix, StringComparison.Ordinal) || !key.EndsWith(suffix, StringComparison.Ordinal)) continue;
            var middle = key.Substring(prefix.Length, key.Length - prefix.Length - suffix.Length);
            if (int.TryParse(middle, out var n) && n > max) max = n;
        }
        return max;
    }
}
