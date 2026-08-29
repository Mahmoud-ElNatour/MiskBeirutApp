using MiskBeirut.Application.Dtos.Pages;

namespace MiskBeirut.Web.Support;

/// <summary>
/// Read-only view over a backoffice (Admin-area) page's CMS attributes, plus the shared "Global"
/// page's attributes for nav/shared chrome content. English-only — the Admin area has no
/// language switcher, unlike the public site's <see cref="PageContent"/>.
/// </summary>
public sealed class BackofficePageContent
{
    private readonly Dictionary<string, string?> _page;
    private readonly Dictionary<string, string?> _global;

    public BackofficePageContent(BackofficePageDto? page, BackofficePageDto? global)
    {
        _page = ToDictionary(page);
        _global = ToDictionary(global);
    }

    private static Dictionary<string, string?> ToDictionary(BackofficePageDto? page)
        => page?.Attributes.ToDictionary(a => a.AttributeName, a => a.Value)
           ?? new Dictionary<string, string?>();

    public string Text(string name, string fallback = "") => _page.TryGetValue(name, out var v) && !string.IsNullOrEmpty(v) ? v! : fallback;

    public string Image(string name, string fallback = "") => Text(name, fallback);

    public string Link(string name, string fallback = "#") => Text(name, fallback);

    public bool Has(string name) => _page.TryGetValue(name, out var v) && !string.IsNullOrEmpty(v);

    public string Global(string name, string fallback = "") => _global.TryGetValue(name, out var v) && !string.IsNullOrEmpty(v) ? v! : fallback;
}
