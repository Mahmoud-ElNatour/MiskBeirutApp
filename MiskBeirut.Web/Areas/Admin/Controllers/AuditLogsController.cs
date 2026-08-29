using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiskBeirut.Application.Managers;
using MiskBeirut.Core.Constants;
using MiskBeirut.Web.Authorization;

namespace MiskBeirut.Web.Areas.Admin.Controllers;

[RequirePrivilege("AuditLogs")]
public class AuditLogsController : AdminControllerBase
{
    private readonly AuditLogManager _auditLogs;

    public AuditLogsController(AuditLogManager auditLogs, BackofficePageContentManager pages) : base(pages)
    {
        _auditLogs = auditLogs;
    }

    public async Task<IActionResult> Index(string? entityType, string? auditAction, int? month, int? year)
    {
        await LoadPageAsync("AuditLogs");

        var logs = await _auditLogs.GetAsync(month, year);

        if (!string.IsNullOrWhiteSpace(entityType))
            logs = logs.Where(l => l.EntityType == entityType).ToList();
        if (!string.IsNullOrWhiteSpace(auditAction))
            logs = logs.Where(l => l.Action == auditAction).ToList();

        ViewData["EntityType"] = entityType;
        ViewData["Action"] = auditAction;
        ViewData["CurrentMonth"] = month;
        ViewData["CurrentYear"] = year;
        ViewData["EntityTypes"] = new[] { "User", "DailyClosing", "Expense", "NonCashPayment", "Customer", "CustomerLedger", "Employee", "EmployeeLedger", "Payroll", "Receiver" };
        ViewData["Actions"] = new[] { "Add", "Update", "Delete" };

        return View(logs);
    }

    // JSON API used by this page's own Details modal (loadDetails() in Index.cshtml).
    [HttpGet("/api/audit-logs/detail/{id:int}")]
    public async Task<IActionResult> DetailJson(int id)
    {
        var log = await _auditLogs.GetByIdAsync(id);
        if (log is null)
            return NotFound();

        return Json(log);
    }
}
