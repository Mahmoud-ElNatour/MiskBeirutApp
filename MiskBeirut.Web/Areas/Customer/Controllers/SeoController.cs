using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using MiskBeirut.Web.Support;

namespace MiskBeirut.Web.Areas.Customer.Controllers;

/// <summary>
/// The two files crawlers fetch by convention: /robots.txt and /sitemap.xml.
///
/// Served from the app rather than dropped into wwwroot as static files, because both have to
/// answer differently depending on the host. All three subdomains are one site on one set of IIS
/// bindings, so a static /robots.txt would be the same file on backoffice.miskbeirut.com as on the
/// public site — inviting a crawler into the back office and pointing it at a sitemap of pages that
/// don't exist there. Attribute routes, so they answer at the root with no language prefix; the
/// language-prefixing middleware leaves both paths alone for the same reason.
/// </summary>
[Area("Customer")]
public class SeoController : Controller
{
    private static readonly XNamespace Sitemap = "http://www.sitemaps.org/schemas/sitemap/0.9";
    private static readonly XNamespace Xhtml = "http://www.w3.org/1999/xhtml";

    private readonly SiteUrls _urls;

    public SeoController(SiteUrls urls)
    {
        _urls = urls;
    }

    [HttpGet("/robots.txt")]
    [ResponseCache(Duration = 3600)]
    public IActionResult Robots()
    {
        // The back office and the CMS are login-only and hold employee, payroll and applicant data.
        // Nothing there should be fetched by a crawler even to be told it needs a password.
        if (!PublicUrlMiddleware.IsPublicHost(Request.Host))
            return Content("User-agent: *\nDisallow: /\n", "text/plain");

        var lines = new List<string>
        {
            "User-agent: *",

            // Named explicitly because blocking the CSS and JS a page needs to render makes Google
            // judge it as the unstyled version — a common and self-inflicted mobile-usability hit.
            "Allow: /css/",
            "Allow: /js/",
            "Allow: /img/",
            "Allow: /lib/",
            "Allow: /pdf/",

            // Form handlers. They only answer POST, so a crawler following one gets a 405; there is
            // nothing to index and no reason to spend crawl budget finding that out.
            "Disallow: /en/leads/",
            "Disallow: /ar/leads/",
            "Disallow: /en/contact/submit",
            "Disallow: /ar/contact/submit",
            "Disallow: /en/careers/apply",
            "Disallow: /ar/careers/apply",
            "",
            $"Sitemap: {_urls.Absolute(Request, "/sitemap.xml")}",
            ""
        };

        return Content(string.Join("\n", lines), "text/plain");
    }

    /// <summary>
    /// Every public page in every language, each entry cross-referencing the other languages with
    /// xhtml:link. Those alternates are the sitemap's half of the same statement the hreflang tags
    /// in the page head make; Google checks the two against each other, so both are generated from
    /// the same list (<see cref="PublicPages.All"/>) rather than maintained separately.
    /// </summary>
    [HttpGet("/sitemap.xml")]
    [ResponseCache(Duration = 3600)]
    public IActionResult Sitemap_Xml()
    {
        if (!PublicUrlMiddleware.IsPublicHost(Request.Host))
            return NotFound();

        var urlSet = new XElement(Sitemap + "urlset", new XAttribute(XNamespace.Xmlns + "xhtml", Xhtml));

        foreach (var page in PublicPages.All)
        {
            foreach (var lang in SiteLanguages.All)
            {
                var entry = new XElement(Sitemap + "url",
                    new XElement(Sitemap + "loc", _urls.ForLanguage(Request, lang, page.Path)),
                    new XElement(Sitemap + "changefreq", page.ChangeFrequency),
                    new XElement(Sitemap + "priority", page.Priority));

                foreach (var alternate in SiteLanguages.All)
                {
                    entry.Add(new XElement(Xhtml + "link",
                        new XAttribute("rel", "alternate"),
                        new XAttribute("hreflang", alternate),
                        new XAttribute("href", _urls.ForLanguage(Request, alternate, page.Path))));
                }

                entry.Add(new XElement(Xhtml + "link",
                    new XAttribute("rel", "alternate"),
                    new XAttribute("hreflang", "x-default"),
                    new XAttribute("href", _urls.ForLanguage(Request, SiteLanguages.Default, page.Path))));

                urlSet.Add(entry);
            }
        }

        var document = new XDocument(new XDeclaration("1.0", "utf-8", null), urlSet);
        return Content(document.Declaration + Environment.NewLine + document, "application/xml");
    }
}
