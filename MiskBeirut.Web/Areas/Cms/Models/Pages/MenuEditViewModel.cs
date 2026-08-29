namespace MiskBeirut.Web.Areas.Cms.Models.Pages;

/// <summary>
/// Backs the Cms "Menu" page's dedicated edit screen: a PDF preview plus an upload control,
/// instead of the generic attribute-row editor. The PDF url itself lives on the shared "Global"
/// page's attributes (see PagesController.MenuPdfAttributeName), not on the Menu page's own
/// attributes, so both page ids are carried here — PageId for the route/language-tab links,
/// GlobalPageId for where the upload actually gets saved.
/// </summary>
public class MenuEditViewModel
{
    public int PageId { get; set; }
    public int GlobalPageId { get; set; }

    public int LangId { get; set; }
    public string LangCode { get; set; } = "en";
    public List<LanguageOptionViewModel> Languages { get; set; } = [];

    public string? PdfUrl { get; set; }
}