using Microsoft.AspNetCore.Mvc;

namespace MiskBeirut.Web.Areas.Admin.Controllers;

public class HomeController : AdminControllerBase
{
    public IActionResult Index() => View();
}
