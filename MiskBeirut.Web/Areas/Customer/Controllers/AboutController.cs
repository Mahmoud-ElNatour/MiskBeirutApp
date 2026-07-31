using Microsoft.AspNetCore.Mvc;
using MiskBeirut.Application.Managers;
using MiskBeirut.Core.Repositories;

namespace MiskBeirut.Web.Areas.Customer.Controllers;

public class AboutController : PublicContentController
{
    public AboutController(PageContentManager pages, ILanguageRepository languages)
        : base(pages, languages)
    {
    }

    public async Task<IActionResult> Index()
    {
        var content = await LoadPageAsync("About");
        return View(content);
    }
}
