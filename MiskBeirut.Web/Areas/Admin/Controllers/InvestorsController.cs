using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiskBeirut.Application.Dtos.Investors;
using MiskBeirut.Application.Managers;
using MiskBeirut.Core.Constants;
using MiskBeirut.Core.Enums;
using MiskBeirut.Web.Areas.Admin.Models.Investors;
using MiskBeirut.Web.Authorization;

namespace MiskBeirut.Web.Areas.Admin.Controllers;

[RequirePrivilege("Investors")]
public class InvestorsController : AdminControllerBase
{
    private readonly InvestorManager _investors;
    private readonly ReceiverManager _receivers;
    private readonly DailyClosingManager _dailyClosings;
    private readonly AuditLogManager _auditLogs;

    public InvestorsController(
        InvestorManager investors,
        ReceiverManager receivers,
        DailyClosingManager dailyClosings,
        AuditLogManager auditLogs,
        BackofficePageContentManager pages) : base(pages)
    {
        _investors = investors;
        _receivers = receivers;
        _dailyClosings = dailyClosings;
        _auditLogs = auditLogs;
    }

    public async Task<IActionResult> Index()
    {
        await LoadPageAsync("Investors");

        ViewData["InactiveInvestors"] = await BuildListItemsAsync(await _investors.GetInactiveAsync());
        return View(await BuildListItemsAsync(await _investors.GetActiveAsync()));
    }

    private async Task<List<InvestorListItemViewModel>> BuildListItemsAsync(IReadOnlyList<InvestorDto> investors)
    {
        var items = new List<InvestorListItemViewModel>();
        foreach (var investor in investors)
        {
            var transactions = await _investors.GetTransactionsAsync(investor.Id);
            items.Add(new InvestorListItemViewModel
            {
                Id = investor.Id,
                Name = investor.Name,
                TotalExpenses = transactions.Where(t => t.TransactionType == InvestorTransactionType.Expense).Sum(t => t.Amount),
                TotalWithdrawals = transactions.Where(t => t.TransactionType == InvestorTransactionType.Withdrawal).Sum(t => t.Amount)
            });
        }
        return items;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        try
        {
            var before = await _investors.GetByIdAsync(id);
            await _investors.DeactivateAsync(id);
            var after = await _investors.GetByIdAsync(id);
            await _auditLogs.LogAsync("Investor", "Deactivate", id.ToString(), CurrentUserId, CurrentUsername,
                $"Deactivated investor {id}.", oldValues: AuditJson(before), newValues: AuditJson(after));
            TempData["Success"] = "Investor deactivated. Their history is untouched — reactivate them any time from the Inactive Investors list below.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reactivate(int id)
    {
        try
        {
            var before = await _investors.GetByIdAsync(id);
            await _investors.ReactivateAsync(id);
            var after = await _investors.GetByIdAsync(id);
            await _auditLogs.LogAsync("Investor", "Reactivate", id.ToString(), CurrentUserId, CurrentUsername,
                $"Reactivated investor {id}.", oldValues: AuditJson(before), newValues: AuditJson(after));
            TempData["Success"] = "Investor reactivated.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Create() => View(new InvestorFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InvestorFormViewModel request)
    {
        if (!ModelState.IsValid)
            return View(request);

        var investor = await _investors.CreateAsync(new CreateInvestorRequest { Name = request.Name });
        await _auditLogs.LogAsync("Investor", "Add", investor.Id.ToString(), CurrentUserId, CurrentUsername,
            $"Created investor '{investor.Name}'.", newValues: AuditJson(investor));

        TempData["Success"] = $"Investor '{investor.Name}' created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id, int? month, int? year)
    {
        await LoadPageAsync("Investors");

        var investor = await _investors.GetByIdAsync(id);
        if (investor is null)
            return NotFound();

        var transactions = await _investors.GetTransactionsAsync(id);
        if (month.HasValue)
            transactions = transactions.Where(t => t.Date.Month == month.Value).ToList();
        if (year.HasValue)
            transactions = transactions.Where(t => t.Date.Year == year.Value).ToList();

        var expenses = transactions.Where(t => t.TransactionType == InvestorTransactionType.Expense).ToList();
        var withdrawals = transactions.Where(t => t.TransactionType == InvestorTransactionType.Withdrawal).ToList();

        ViewData["CurrentMonth"] = month;
        ViewData["CurrentYear"] = year;

        var receiverBreakdown = expenses
            .GroupBy(t => new { t.ReceiverId, t.ReceiverName })
            .Where(g => g.Key.ReceiverId.HasValue)
            .Select(g => new InvestorReceiverBreakdownItem
            {
                ReceiverId = g.Key.ReceiverId!.Value,
                ReceiverName = g.Key.ReceiverName ?? "Unknown",
                Total = g.Sum(t => t.Amount)
            })
            .OrderByDescending(r => r.Total)
            .ToList();

        var vm = new InvestorDetailsViewModel
        {
            Investor = investor,
            Expenses = expenses,
            Withdrawals = withdrawals,
            ReceiverBreakdown = receiverBreakdown,
            Receivers = await _receivers.GetAllAsync(),
            RecentClosings = (await _dailyClosings.GetAllAsync()).Take(60).ToList(),
            NewTransaction = new AddInvestorTransactionViewModel { InvestorId = id }
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTransaction([Bind(Prefix = "NewTransaction")] AddInvestorTransactionViewModel request)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Please fill in all required fields.";
            return RedirectToAction(nameof(Details), new { id = request.InvestorId });
        }

        try
        {
            var transaction = await _investors.AddTransactionAsync(new CreateInvestorTransactionRequest
            {
                Date = DateOnly.FromDateTime(DateTime.Today),
                Amount = request.Amount,
                TransactionType = request.TransactionType,
                Note = request.Note,
                DailyClosingId = request.DailyClosingId,
                InvestorId = request.InvestorId,
                ReceiverId = request.ReceiverId
            });

            await _auditLogs.LogAsync("InvestorTransaction", "Add", transaction.Id.ToString(), CurrentUserId, CurrentUsername,
                $"Added {transaction.TransactionType} {transaction.Amount:N2} for investor {request.InvestorId}.", newValues: AuditJson(transaction));

            TempData["Success"] = "Transaction added.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id = request.InvestorId });
    }
}
