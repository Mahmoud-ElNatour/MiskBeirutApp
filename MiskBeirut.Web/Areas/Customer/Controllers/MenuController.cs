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

    public async Task<IActionResult> Index()
    {
        var content = await LoadPageAsync("Menu");
        return View(content);
    }
}
