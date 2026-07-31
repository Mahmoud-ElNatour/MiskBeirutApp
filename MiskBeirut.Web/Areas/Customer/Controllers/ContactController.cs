using Microsoft.AspNetCore.Mvc;
using MiskBeirut.Application.Managers;
using MiskBeirut.Core.Repositories;

namespace MiskBeirut.Web.Areas.Customer.Controllers;

public class ContactController : PublicContentController
{
    public ContactController(PageContentManager pages, ILanguageRepository languages)
        : base(pages, languages)
    {
    }

    public async Task<IActionResult> Index()
    {
        var content = await LoadPageAsync("Contact");
        return View(content);
    }
}
