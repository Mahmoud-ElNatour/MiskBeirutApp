using Microsoft.AspNetCore.Mvc;
using MiskBeirut.Application.Managers;

namespace MiskBeirut.Web.Areas.Admin.Controllers;

/// <summary>Retired — this page was never wired to a real data source (its JS expected a /api/logs
/// endpoint that didn't exist, so it always failed to load) and duplicated what Audit Logs already
/// does. Kept only so any stale link/bookmark lands somewhere useful instead of a 404.</summary>
public class LogsController : AdminControllerBase
{
    public LogsController(BackofficePageContentManager pages) : base(pages)
    {
    }

    public IActionResult Index() => RedirectToAction("Index", "AuditLogs");
}
