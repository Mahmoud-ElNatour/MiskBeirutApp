using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MiskBeirut.Application.Managers;
using MiskBeirut.Core.Repositories;
using MiskBeirut.Web.Models;

namespace MiskBeirut.Web.Areas.Customer.Controllers;

public class HomeController : PublicContentController
{
    private readonly ILogger<HomeController> _logger;
    private readonly GoogleReviewManager _reviews;

    public HomeController(ILogger<HomeController> logger, PageContentManager pages, ILanguageRepository languages, GoogleReviewManager reviews)
        : base(pages, languages)
    {
        _logger = logger;
        _reviews = reviews;
    }

    public async Task<IActionResult> Index()
    {
        var content = await LoadPageAsync("Home");
        ViewData["GoogleReviews"] = await _reviews.GetFeaturedAsync(3);
        return View(content);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
