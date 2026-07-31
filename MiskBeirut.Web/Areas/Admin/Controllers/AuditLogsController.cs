using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiskBeirut.Application.Managers;
using MiskBeirut.Core.Constants;

namespace MiskBeirut.Web.Areas.Admin.Controllers;

[Authorize(Roles = RoleNames.Admin)]
public class AuditLogsController : AdminControllerBase
{
    private readonly AuditLogManager _auditLogs;

    public AuditLogsController(AuditLogManager auditLogs)
    {
        _auditLogs = auditLogs;
    }

    public async Task<IActionResult> Index(string? entityType, string? auditAction)
    {
        var logs = await _auditLogs.GetRecentAsync(300);

        if (!string.IsNullOrWhiteSpace(entityType))
            logs = logs.Where(l => l.EntityType == entityType).ToList();
        if (!string.IsNullOrWhiteSpace(auditAction))
            logs = logs.Where(l => l.Action == auditAction).ToList();

        ViewData["EntityType"] = entityType;
        ViewData["Action"] = auditAction;
        ViewData["EntityTypes"] = new[] { "User", "DailyClosing", "Expense", "NonCashPayment", "Customer", "CustomerLedger", "Employee", "EmployeeLedger", "Payroll", "Receiver" };
        ViewData["Actions"] = new[] { "Add", "Update", "Delete" };

        return View(logs);
    }
}
