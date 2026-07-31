using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiskBeirut.Application.Dtos.DailyClosings;
using MiskBeirut.Application.Managers;
using MiskBeirut.Core.Constants;
using MiskBeirut.Web.Areas.Admin.Models.DailyClosing;

namespace MiskBeirut.Web.Areas.Admin.Controllers;

[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Supervisor},{RoleNames.Employee}")]
public class DailyClosingController : AdminControllerBase
{
    private readonly DailyClosingManager _closings;
    private readonly ExpenseManager _expenses;
    private readonly NonCashPaymentManager _nonCashPayments;
    private readonly ReceiverManager _receivers;
    private readonly AuditLogManager _auditLogs;

    public DailyClosingController(
        DailyClosingManager closings,
        ExpenseManager expenses,
        NonCashPaymentManager nonCashPayments,
        ReceiverManager receivers,
        AuditLogManager auditLogs)
    {
        _closings = closings;
        _expenses = expenses;
        _nonCashPayments = nonCashPayments;
        _receivers = receivers;
        _auditLogs = auditLogs;
    }

    /// <summary>Employee may only ever act on today's date — never a prior day, never a future one.</summary>
    private bool CanAccessDate(DateOnly date)
    {
        if (User.IsInRole(RoleNames.Admin) || User.IsInRole(RoleNames.Supervisor))
            return true;

        return User.IsInRole(RoleNames.Employee) && date == DateOnly.FromDateTime(DateTime.Today);
    }

    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Supervisor}")]
    public async Task<IActionResult> Index(int? year, int? month)
    {
        var closings = await _closings.GetAllAsync(year, month);
        return View(closings);
    }

    public async Task<IActionResult> Details(int id)
    {
        var closing = await _closings.GetByIdAsync(id);
        if (closing is null)
            return NotFound();

        if (!CanAccessDate(closing.Date))
            return Forbid();

        var vm = new DailyClosingDetailsViewModel
        {
            Closing = closing,
            Summary = await _closings.GetSummaryAsync(id),
            Expenses = await _expenses.GetByDailyClosingAsync(id),
            NonCashPayments = await _nonCashPayments.GetByDailyClosingAsync(id),
            Receivers = await _receivers.GetAllAsync(),
            NewExpense = new AddExpenseViewModel { DailyClosingId = id },
            NewNonCashPayment = new AddNonCashPaymentViewModel { DailyClosingId = id },
            CanManageHeader = User.IsInRole(RoleNames.Admin) || User.IsInRole(RoleNames.Supervisor)
        };
        return View(vm);
    }

    [HttpGet]
    public IActionResult Create()
    {
        var vm = new CreateDailyClosingViewModel();
        if (User.IsInRole(RoleNames.Employee) && !User.IsInRole(RoleNames.Admin) && !User.IsInRole(RoleNames.Supervisor))
            vm.Date = DateOnly.FromDateTime(DateTime.Today);

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateDailyClosingViewModel request)
    {
        var employeeOnly = User.IsInRole(RoleNames.Employee) && !User.IsInRole(RoleNames.Admin) && !User.IsInRole(RoleNames.Supervisor);
        if (employeeOnly)
            request.Date = DateOnly.FromDateTime(DateTime.Today);

        if (!ModelState.IsValid)
            return View(request);

        try
        {
            var closing = await _closings.CreateAsync(new CreateDailyClosingRequest
            {
                Date = request.Date,
                MainReading = request.MainReading,
                AdjustedReading = request.AdjustedReading,
                ActualCash = request.ActualCash,
                Note = request.Note
            });

            await _auditLogs.LogAsync("DailyClosing", "Add", closing.Id.ToString(), CurrentUserId, CurrentUsername,
                $"Created daily closing for {closing.Date:yyyy-MM-dd}.");

            TempData["Success"] = $"Daily closing for {closing.Date:yyyy-MM-dd} created.";
            return RedirectToAction(nameof(Details), new { id = closing.Id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(request);
        }
    }

    [HttpGet]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Supervisor}")]
    public async Task<IActionResult> Edit(int id)
    {
        var closing = await _closings.GetByIdAsync(id);
        if (closing is null)
            return NotFound();

        return View(new EditDailyClosingViewModel
        {
            Id = closing.Id,
            Date = closing.Date,
            MainReading = closing.MainReading,
            AdjustedReading = closing.AdjustedReading,
            ActualCash = closing.ActualCash,
            Note = closing.Note
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Supervisor}")]
    public async Task<IActionResult> Edit(int id, EditDailyClosingViewModel request)
    {
        if (id != request.Id)
            return BadRequest();

        await _closings.UpdateAsync(id, new UpdateDailyClosingRequest
        {
            AdjustedReading = request.AdjustedReading,
            ActualCash = request.ActualCash,
            Note = request.Note
        });

        await _auditLogs.LogAsync("DailyClosing", "Update", id.ToString(), CurrentUserId, CurrentUsername, "Edited daily closing header.");

        TempData["Success"] = "Daily closing updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddExpense([Bind(Prefix = "NewExpense")] AddExpenseViewModel request)
    {
        var closing = await _closings.GetByIdAsync(request.DailyClosingId);
        if (closing is null)
            return NotFound();
        if (!CanAccessDate(closing.Date))
            return Forbid();

        var expense = await _expenses.AddAsync(new CreateExpenseRequest
        {
            Date = closing.Date,
            Amount = request.Amount,
            Note = request.Note,
            DailyClosingId = request.DailyClosingId,
            ReceiverId = request.ReceiverId
        });

        await _auditLogs.LogAsync("Expense", "Add", expense.Id.ToString(), CurrentUserId, CurrentUsername,
            $"Added expense {expense.Amount:N2} to daily closing {request.DailyClosingId}.");

        return RedirectToAction(nameof(Details), new { id = request.DailyClosingId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Supervisor}")]
    public async Task<IActionResult> DeleteExpense(int id, int dailyClosingId)
    {
        await _expenses.DeleteAsync(id);
        await _auditLogs.LogAsync("Expense", "Delete", id.ToString(), CurrentUserId, CurrentUsername,
            $"Deleted expense from daily closing {dailyClosingId}.");
        return RedirectToAction(nameof(Details), new { id = dailyClosingId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddNonCashPayment([Bind(Prefix = "NewNonCashPayment")] AddNonCashPaymentViewModel request)
    {
        var closing = await _closings.GetByIdAsync(request.DailyClosingId);
        if (closing is null)
            return NotFound();
        if (!CanAccessDate(closing.Date))
            return Forbid();

        var payment = await _nonCashPayments.AddAsync(new CreateNonCashPaymentRequest
        {
            Date = closing.Date,
            Amount = request.Amount,
            PaymentMethod = request.PaymentMethod,
            Note = request.Note,
            DailyClosingId = request.DailyClosingId
        });

        await _auditLogs.LogAsync("NonCashPayment", "Add", payment.Id.ToString(), CurrentUserId, CurrentUsername,
            $"Added {payment.PaymentMethod} payment {payment.Amount:N2} to daily closing {request.DailyClosingId}.");

        return RedirectToAction(nameof(Details), new { id = request.DailyClosingId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Supervisor}")]
    public async Task<IActionResult> DeleteNonCashPayment(int id, int dailyClosingId)
    {
        await _nonCashPayments.DeleteAsync(id);
        await _auditLogs.LogAsync("NonCashPayment", "Delete", id.ToString(), CurrentUserId, CurrentUsername,
            $"Deleted non-cash payment from daily closing {dailyClosingId}.");
        return RedirectToAction(nameof(Details), new { id = dailyClosingId });
    }
}
