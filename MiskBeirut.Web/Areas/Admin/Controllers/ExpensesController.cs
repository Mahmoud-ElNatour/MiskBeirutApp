using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiskBeirut.Application.Dtos.DailyClosings;
using MiskBeirut.Application.Managers;
using MiskBeirut.Core.Constants;
using MiskBeirut.Web.Areas.Admin.Models.Expenses;
using MiskBeirut.Web.Authorization;

namespace MiskBeirut.Web.Areas.Admin.Controllers;

[RequirePrivilege("Expenses")]
public class ExpensesController : AdminControllerBase
{
    private readonly ExpenseManager _expenses;
    private readonly ReceiverManager _receivers;
    private readonly InvestorManager _investors;
    private readonly AuditLogManager _auditLogs;

    public ExpensesController(ExpenseManager expenses, ReceiverManager receivers, InvestorManager investors, AuditLogManager auditLogs, BackofficePageContentManager pages) : base(pages)
    {
        _expenses = expenses;
        _receivers = receivers;
        _investors = investors;
        _auditLogs = auditLogs;
    }

    public async Task<IActionResult> Index(int? month, int? year, int? receiverId)
    {
        await LoadPageAsync("Expenses");

        var records = await _expenses.GetReportAsync(month, year, receiverId);

        ViewData["CurrentMonth"] = month;
        ViewData["CurrentYear"] = year;
        ViewData["CurrentReceiverId"] = receiverId;
        ViewData["Receivers"] = await _receivers.GetAllAsync();
        ViewData["Total"] = records.Sum(r => r.Amount);
        ViewData["Count"] = records.Count;
        ViewData["Max"] = records.Count > 0 ? records.Max(r => r.Amount) : 0;

        return View(records);
    }

    /// <summary>
    /// Adds a one-off expense with no Daily Closing yet — DailyClosingManager attaches it
    /// automatically once a closing for that date is created or edited.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePrivilege("Expenses")]
    public async Task<IActionResult> Add(AddExpenseViewModel request)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Please fill in the date, amount and receiver.";
            return RedirectToAction(nameof(Index));
        }

        var expense = await _expenses.AddAsync(new CreateExpenseRequest
        {
            Date = request.Date,
            Amount = request.Amount,
            Note = request.Note,
            ReceiverId = request.ReceiverId,
            DailyClosingId = null,
            IsManualEntry = true
        });

        await _auditLogs.LogAsync("Expense", "Add", expense.Id.ToString(), CurrentUserId, CurrentUsername,
            $"Added manual expense {expense.Amount:N2} for {request.Date:yyyy-MM-dd}.", newValues: AuditJson(expense));

        TempData["Success"] = "Expense added.";
        return RedirectToAction(nameof(Index));
    }

    // "Ahmad"/"Samer" personal expense tracking from the old app is superseded by the generic
    // Investors feature (Areas/Admin/Controllers/InvestorsController.cs) — these two routes are
    // kept only because old bookmarks/links may still point at them, and now just redirect to the
    // matching investor's Details page instead of maintaining a parallel, duplicate view.
    public Task<IActionResult> Ahmad() => RedirectToInvestorAsync("Ahmad");

    public Task<IActionResult> Samer() => RedirectToInvestorAsync("Samer");

    private async Task<IActionResult> RedirectToInvestorAsync(string investorName)
    {
        var investors = await _investors.GetActiveAsync();
        var investor = investors.FirstOrDefault(i => i.Name.Contains(investorName, StringComparison.OrdinalIgnoreCase));

        if (investor is null)
        {
            TempData["Error"] = $"No investor named '{investorName}' was found. Create one first.";
            return RedirectToAction("Index", "Investors");
        }

        return RedirectToAction("Details", "Investors", new { id = investor.Id });
    }
}
