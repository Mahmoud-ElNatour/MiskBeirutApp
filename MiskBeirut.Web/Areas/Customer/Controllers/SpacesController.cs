using Microsoft.AspNetCore.Mvc;
using MiskBeirut.Application.Managers;
using MiskBeirut.Core.Repositories;
using MiskBeirut.Web.Support;

namespace MiskBeirut.Web.Areas.Customer.Controllers;

public class SpacesController : PublicContentController
{
    public SpacesController(PageContentManager pages, ILanguageRepository languages, SiteUrls urls)
        : base(pages, languages, urls)
    {
    }

    public async Task<IActionResult> Index()
    {
        var content = await LoadPageAsync("Spaces");
        return View(content);
    }
}
