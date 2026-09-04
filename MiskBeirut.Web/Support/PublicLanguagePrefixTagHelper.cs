using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace MiskBeirut.Web.Support;

/// <summary>
/// Guarantees that every link the public site generates keeps its language segment.
///
/// The route is "{lang}/{controller}/{action}", and the language is sitting right there in the
/// current request's route values — but ASP.NET's link generation does not carry it across a
/// controller change. Asking for the Events page from the About page discards the ambient "lang"
/// along with everything else the new controller invalidates, and the binder then emits the URL
/// without that segment: on /ar/about, every nav link came out as "/about", "/events", "/menu".
/// Those are not broken links (PublicUrlMiddleware redirects them) but they are the wrong ones —
/// a visitor reading Arabic was one click from being bounced back into English, and a crawler was
/// handed an unprefixed, redirecting duplicate of every page on the site.
///
/// Supplying asp-area and asp-route-lang on each link also fixes it, and that is what this does in
/// effect — just once, here, instead of on thirty-odd anchors that a future page has to remember to
/// copy. It runs after the anchor and form tag helpers have written their URL and prefixes what
/// they produced, which is the same normalization PublicUrlMiddleware applies to incoming URLs.
/// </summary>
[HtmlTargetElement("a", Attributes = "asp-controller")]
[HtmlTargetElement("a", Attributes = "asp-action")]
[HtmlTargetElement("form", Attributes = "asp-controller")]
[HtmlTargetElement("form", Attributes = "asp-action")]
public sealed class PublicLanguagePrefixTagHelper : TagHelper
{
    /// <summary>Runs last: AnchorTagHelper and FormTagHelper must have written href/action first.</summary>
    public override int Order => int.MaxValue;

    [HtmlAttributeNotBound]
    [ViewContext]
    public ViewContext ViewContext { get; set; } = null!;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        // Public pages only. The Cms renders these same views inside its visual preview, where the
        // area is Cms and the links are internal to the preview — prefixing them there would point
        // an editor at the live site.
        if (ViewContext.RouteData.Values["area"] as string != "Customer")
            return;

        var attributeName = output.TagName == "form" ? "action" : "href";
        if (!output.Attributes.TryGetAttribute(attributeName, out var attribute))
            return;

        var url = attribute.Value as string ?? attribute.Value?.ToString();
        if (url is null || !NeedsPrefix(url))
            return;

        var lang = SiteLanguages.Normalize(ViewContext.RouteData.Values["lang"] as string);
        output.Attributes.SetAttribute(attributeName, $"/{lang}{(url == "/" ? "" : url)}");
    }

    private static bool NeedsPrefix(string url)
    {
        // Site-relative only. "//cdn.example.com/x" is protocol-relative and belongs to someone else.
        if (!url.StartsWith('/') || url.StartsWith("//"))
            return false;

        var firstSegment = url.TrimStart('/').Split('/', '?', '#')[0];
        return !SiteLanguages.IsSupported(firstSegment);
    }
}
