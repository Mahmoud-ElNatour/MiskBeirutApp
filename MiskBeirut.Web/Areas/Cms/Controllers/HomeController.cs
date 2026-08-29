using Microsoft.AspNetCore.Mvc;
using MiskBeirut.Application.Managers;

namespace MiskBeirut.Web.Areas.Cms.Controllers;

public class HomeController : CmsControllerBase
{
    private readonly PageContentManager _pages;

    public HomeController(PageContentManager pages)
    {
        _pages = pages;
    }

    public async Task<IActionResult> Index()
    {
        var pages = await _pages.GetAllPagesAsync();
        return View(pages);
    }
}
