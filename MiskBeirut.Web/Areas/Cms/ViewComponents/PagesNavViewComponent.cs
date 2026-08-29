using Microsoft.AspNetCore.Mvc;
using MiskBeirut.Application.Managers;

namespace MiskBeirut.Web.Areas.Cms.ViewComponents;

/// <summary>Sidebar's "Pages" accordion — the list of editable pages, shared across every Cms view.</summary>
public class PagesNavViewComponent : ViewComponent
{
    private readonly PageContentManager _pages;

    public PagesNavViewComponent(PageContentManager pages)
    {
        _pages = pages;
    }

    public async Task<IViewComponentResult> InvokeAsync(int? currentPageId)
    {
        var pages = await _pages.GetAllPagesAsync();
        ViewBag.CurrentPageId = currentPageId;
        return View(pages.OrderBy(p => p.PageName).ToList());
    }
}
