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
}
