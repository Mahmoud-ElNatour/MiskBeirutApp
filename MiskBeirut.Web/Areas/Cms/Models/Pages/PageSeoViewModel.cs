using System.ComponentModel.DataAnnotations;

namespace MiskBeirut.Web.Areas.Cms.Models.Pages;

/// <summary>
/// One page's SEO metadata in one language. Lengths mirror the customer.pages column limits, which
/// the default language's values are also written to — see MiskBeirut.Web.Support.SeoAttributes.
/// </summary>
public class PageSeoViewModel
{
    public int PageId { get; set; }
    public string PageName { get; set; } = "";

    public int LangId { get; set; }
    public string LangCode { get; set; } = "en";

    /// <summary>True for the site's default language, whose values also update the page's own columns.</summary>
    public bool IsDefaultLanguage { get; set; }

    public List<LanguageOptionViewModel> Languages { get; set; } = [];

    [StringLength(300, ErrorMessage = "Meta title must be 300 characters or fewer.")]
    public string? MetaTitle { get; set; }

    [StringLength(500, ErrorMessage = "Meta description must be 500 characters or fewer.")]
    public string? MetaDescription { get; set; }

    [StringLength(500, ErrorMessage = "Meta keywords must be 500 characters or fewer.")]
    public string? MetaKeywords { get; set; }
}
