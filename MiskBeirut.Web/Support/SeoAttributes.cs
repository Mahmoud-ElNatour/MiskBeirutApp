namespace MiskBeirut.Web.Support;

/// <summary>
/// Where a page's SEO metadata lives, and how the public site reads it.
///
/// customer.pages has MetaTitle/MetaDesc/MetaKeyword columns, but they hold ONE value per page —
/// on a bilingual site that means the Arabic version of a page advertises an English title and
/// description to search engines. So the editable values are per-language attribute rows like any
/// other content, and the columns are kept as the language-neutral fallback (and as what the Cms
/// page list and dashboard show at a glance): saving the default language writes both.
/// </summary>
public static class SeoAttributes
{
    public const string Title = "meta_title";
    public const string Description = "meta_description";
    public const string Keywords = "meta_keywords";

    /// <summary>The three names above — the generic attribute editor filters these out so they aren't edited in two places.</summary>
    public static readonly string[] All = [Title, Description, Keywords];

    public static bool IsSeoAttribute(string attributeName)
        => All.Contains(attributeName, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The page title for this language: the per-language attribute, then the page's own MetaTitle
    /// column, then "{page} | Misk Beirut" so a page with nothing set still gets a sensible title.
    /// </summary>
    public static string ResolveTitle(PageContent content, string? columnValue, string pageName)
    {
        var value = content.Text(Title, columnValue ?? "");
        return string.IsNullOrWhiteSpace(value) ? $"{pageName} | Misk Beirut" : value;
    }

    /// <summary>The meta description for this language, or null when neither the attribute nor the column is set (the tag is then omitted).</summary>
    public static string? ResolveDescription(PageContent content, string? columnValue)
    {
        var value = content.Text(Description, columnValue ?? "");
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    /// <summary>The meta keywords for this language, or null when unset.</summary>
    public static string? ResolveKeywords(PageContent content, string? columnValue)
    {
        var value = content.Text(Keywords, columnValue ?? "");
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
