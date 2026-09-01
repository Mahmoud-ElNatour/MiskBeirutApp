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
    /// Renders the Menu as a page of the site — nav, framing copy and footer around an embedded
    /// viewer — rather than sending the visitor straight to the raw PDF, which dropped them out of
    /// the site entirely (and, on mobile, into whatever the OS does with a PDF URL). The file itself
    /// still lives in customer.page_attributes (Global/menu_pdf_url) so it can be swapped from the
    /// Cms without a code change; the view falls back to a "coming soon" panel when it's unset.
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var content = await LoadPageAsync("Menu");
        return View(content);
    }
}
