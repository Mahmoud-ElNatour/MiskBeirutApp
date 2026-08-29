using Microsoft.AspNetCore.Mvc;
using MiskBeirut.Application.Managers;
using MiskBeirut.Core.Repositories;

namespace MiskBeirut.Web.Areas.Customer.Controllers;

public class MenuController : PublicContentController
{
    public MenuController(PageContentManager pages, ILanguageRepository languages)
        : base(pages, languages)
    {
    }

    /// <summary>
    /// The Menu nav link no longer renders a page — it redirects straight to the PDF menu file,
    /// whose URL lives in customer.page_attributes (Global/menu_pdf_url) so it can be updated
    /// without a code change. Falls back to the old "menu unavailable" page if that's ever unset.
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var content = await LoadPageAsync("Menu");
        var pdfUrl = content.Global("menu_pdf_url", "");
        if (string.IsNullOrWhiteSpace(pdfUrl))
            return View(content);

        return Redirect(pdfUrl);
    }
}
