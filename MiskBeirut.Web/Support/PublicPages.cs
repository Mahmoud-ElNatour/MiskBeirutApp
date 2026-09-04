namespace MiskBeirut.Web.Support;

/// <summary>
/// The indexable pages of the public site, in the order the sitemap lists them.
///
/// This is a hand-kept list rather than a scan of customer.pages, because the two are not the same
/// set: "Global" is a bucket of shared nav/footer content with no URL of its own, and a page could
/// exist in the CMS before anything routes to it. A sitemap that advertises a URL which 404s is
/// worse than one that is a page short, so the list names what actually has a controller.
///
/// Adding a public page means adding it here — and the sitemap, the language switcher and hreflang
/// then all pick it up together.
/// </summary>
public static class PublicPages
{
    /// <param name="Path">Site-relative, with no language segment. "" is the home page.</param>
    /// <param name="Priority">Relative importance within this site only; it says nothing to Google about ranking against anyone else.</param>
    public sealed record Page(string PageName, string Path, string Priority, string ChangeFrequency);

    public static readonly IReadOnlyList<Page> All =
    [
        new("Home", "", "1.0", "weekly"),
        new("About", "/about", "0.8", "monthly"),
        new("Spaces", "/spaces", "0.8", "monthly"),
        new("Menu", "/menu", "0.9", "monthly"),
        new("Events", "/events", "0.8", "monthly"),
        new("Careers", "/careers", "0.7", "weekly"),
        new("Contact", "/contact", "0.8", "monthly")
    ];
}
