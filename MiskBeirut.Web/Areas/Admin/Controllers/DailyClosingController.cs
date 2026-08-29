using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MiskBeirut.Application.Dtos.Customers;
using MiskBeirut.Application.Dtos.DailyClosings;
using MiskBeirut.Application.Dtos.Employees;
using MiskBeirut.Application.Dtos.Receivers;
using MiskBeirut.Application.Managers;
using MiskBeirut.Core.Constants;
using MiskBeirut.Core.Entities;
using MiskBeirut.Web.Areas.Admin.Models.DailyClosing;
using MiskBeirut.Web.Authorization;

namespace MiskBeirut.Web.Areas.Admin.Controllers;

[RequirePrivilege("DailyClosing")]
public class DailyClosingController : AdminControllerBase
{
    private readonly DailyClosingManager _closings;
    private readonly ExpenseManager _expenses;
    private readonly NonCashPaymentManager _nonCashPayments;
    private readonly ReceiverManager _receivers;
    private readonly InvestorManager _investors;
    private readonly EmployeeManager _employees;
    private readonly CustomerManager _customers;
    private readonly AuditLogManager _auditLogs;
    private readonly SignInManager<User> _signInManager;

    public DailyClosingController(
        DailyClosingManager closings,
        ExpenseManager expenses,
        NonCashPaymentManager nonCashPayments,
        ReceiverManager receivers,
        InvestorManager investors,
        EmployeeManager employees,
        CustomerManager customers,
        AuditLogManager auditLogs,
        SignInManager<User> signInManager,
        BackofficePageContentManager pages) : base(pages)
    {
        _closings = closings;
        _expenses = expenses;
        _nonCashPayments = nonCashPayments;
        _receivers = receivers;
        _investors = investors;
        _employees = employees;
        _customers = customers;
        _auditLogs = auditLogs;
        _signInManager = signInManager;
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
        await LoadPageAsync("DailyClosing");
        var closings = await _closings.GetAllAsync(year, month);
        ViewData["CurrentYear"] = year;
        ViewData["CurrentMonth"] = month;
        return View(closings);
    }

    public async Task<IActionResult> Details(int id)
    {
        await LoadPageAsync("DailyClosingDetails");

        var closing = await _closings.GetByIdAsync(id);
        if (closing is null)
            return NotFound();

        if (!CanAccessDate(closing.Date))
            return Forbid();

        var vm = new DailyClosingDetailsViewModel
        {
            Closing = closing,
            Summary = await _closings.GetSummaryAsync(id),
            Breakdown = await _closings.GetBreakdownAsync(id) ?? new DailyClosingBreakdownDto(),
            Expenses = await _expenses.GetByDailyClosingAsync(id),
            NonCashPayments = await _nonCashPayments.GetByDailyClosingAsync(id),
            Receivers = await _receivers.GetAllAsync()
        };
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadPageAsync("DailyClosingCreate");

        var vm = new CreateDailyClosingViewModel();
        if (User.IsInRole(RoleNames.Employee) && !User.IsInRole(RoleNames.Admin) && !User.IsInRole(RoleNames.Supervisor))
            vm.Date = DateOnly.FromDateTime(DateTime.Today);

        await PopulateDropdownsAsync(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateDailyClosingViewModel request)
    {
        await LoadPageAsync("DailyClosingCreate");

        var employeeOnly = User.IsInRole(RoleNames.Employee) && !User.IsInRole(RoleNames.Admin) && !User.IsInRole(RoleNames.Supervisor);
        if (employeeOnly)
            request.Date = DateOnly.FromDateTime(DateTime.Today);

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(request);
            return View(request);
        }

        try
        {
            var closing = await _closings.CreateWithLinesAsync(new CreateDailyClosingWithLinesRequest
            {
                Date = request.Date,
                MainReading = request.MainReading,
                Note = request.Note,
                GeneralExpenses = request.GeneralExpenses.Select(r => new GeneralExpenseLine
                {
                    ReceiverId = r.ReceiverId,
                    Amount = r.Amount,
                    Note = r.Note
                }).ToList(),
                InvestorExpenses = request.InvestorExpenses.Select(r => new InvestorExpenseLine
                {
                    InvestorId = r.InvestorId,
                    ReceiverId = r.ReceiverId,
                    Amount = r.Amount,
                    Note = r.Note
                }).ToList(),
                Advances = request.Advances.Select(r => new EmployeeLedgerLine
                {
                    EmployeeId = r.EmployeeId,
                    Amount = r.Amount,
                    Note = r.Note
                }).ToList(),
                Deductions = request.Deductions.Select(r => new EmployeeLedgerLine
                {
                    EmployeeId = r.EmployeeId,
                    Amount = r.Amount,
                    Note = r.Note
                }).ToList(),
                Credits = request.Credits.Select(r => new CustomerLedgerLine
                {
                    CustomerId = r.CustomerId,
                    Amount = r.Amount,
                    Note = r.Note
                }).ToList(),
                Cashbacks = request.Cashbacks.Select(r => new CustomerLedgerLine
                {
                    CustomerId = r.CustomerId,
                    Amount = r.Amount,
                    Note = r.Note
                }).ToList(),
                NonCashPayments = request.NonCashPayments.Select(r => new NonCashPaymentLine
                {
                    PaymentMethod = r.PaymentMethod,
                    Amount = r.Amount,
                    Note = r.Note
                }).ToList()
            });

            await _auditLogs.LogAsync("DailyClosing", "Add", closing.Id.ToString(), CurrentUserId, CurrentUsername,
                $"Created daily closing for {closing.Date:yyyy-MM-dd}.", newValues: AuditJson(request));

            TempData["Success"] = $"Daily closing for {closing.Date:yyyy-MM-dd} created.";
            return RedirectToAction(nameof(Details), new { id = closing.Id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateDropdownsAsync(request);
            return View(request);
        }
    }

    private async Task PopulateDropdownsAsync(CreateDailyClosingViewModel vm)
    {
        vm.Receivers = await _receivers.GetAllAsync();
        vm.Investors = await _investors.GetActiveAsync();
        vm.Employees = await _employees.GetActiveAsync();
        vm.Customers = await _customers.GetAllAsync();
    }

    /// <summary>
    /// Co-sign check for the Create page's date-unlock modal: verifies a Supervisor/Admin account's
    /// own credentials without touching the current session (CheckPasswordSignInAsync validates +
    /// honors Identity's lockout policy but never calls SignInAsync).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> VerifyUnlock([FromBody] VerifyUnlockRequest request)
    {
        var user = await _signInManager.UserManager.FindByNameAsync(request.Username ?? "");
        if (user is null)
            return Json(new { ok = false });

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password ?? "", lockoutOnFailure: true);
        if (!result.Succeeded)
            return Json(new { ok = false });

        var roles = await _signInManager.UserManager.GetRolesAsync(user);
        var authorized = roles.Contains(RoleNames.Admin) || roles.Contains(RoleNames.Supervisor);
        return Json(new { ok = authorized });
    }

    [HttpGet]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Supervisor}")]
    public async Task<IActionResult> Edit(int id)
    {
        await LoadPageAsync("DailyClosingEdit");

        var closing = await _closings.GetByIdAsync(id);
        if (closing is null)
            return NotFound();

        var expenses = await _expenses.GetByDailyClosingAsync(id);
        var nonCashPayments = await _nonCashPayments.GetByDailyClosingAsync(id);
        var breakdown = await _closings.GetBreakdownAsync(id) ?? new DailyClosingBreakdownDto();

        var vm = new EditDailyClosingViewModel
        {
            Id = closing.Id,
            Date = closing.Date,
            MainReading = closing.MainReading,
            Note = closing.Note,
            GeneralExpenses = expenses.Select(e => new GeneralExpenseRowViewModel
            {
                ReceiverId = e.ReceiverId,
                Amount = e.Amount,
                Note = e.Note
            }).ToList(),
            InvestorExpenses = breakdown.InvestorExpenses.Select(t => new InvestorExpenseRowViewModel
            {
                InvestorId = t.InvestorId,
                ReceiverId = t.ReceiverId ?? 0,
                Amount = t.Amount,
                Note = t.Note
            }).ToList(),
            Advances = breakdown.Advances.Select(a => new EmployeeLedgerRowViewModel
            {
                EmployeeId = a.EmployeeId,
                Amount = Math.Abs(a.Amount),
                Note = a.Note
            }).ToList(),
            Deductions = breakdown.Deductions.Select(d => new EmployeeLedgerRowViewModel
            {
                EmployeeId = d.EmployeeId,
                Amount = Math.Abs(d.Amount),
                Note = d.Note
            }).ToList(),
            Credits = breakdown.Credits.Select(c => new CustomerLedgerRowViewModel
            {
                CustomerId = c.CustomerId,
                Amount = Math.Abs(c.Amount),
                Note = c.Note
            }).ToList(),
            Cashbacks = breakdown.Cashbacks.Select(c => new CustomerLedgerRowViewModel
            {
                CustomerId = c.CustomerId,
                Amount = Math.Abs(c.Amount),
                Note = c.Note
            }).ToList(),
            NonCashPayments = nonCashPayments.Select(p => new NonCashPaymentRowViewModel
            {
                PaymentMethod = p.PaymentMethod,
                Amount = p.Amount,
                Note = p.Note
            }).ToList()
        };

        await PopulateEditDropdownsAsync(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Supervisor}")]
    public async Task<IActionResult> Edit(int id, EditDailyClosingViewModel request)
    {
        await LoadPageAsync("DailyClosingEdit");

        if (id != request.Id)
            return BadRequest();

        if (!ModelState.IsValid)
        {
            await PopulateEditDropdownsAsync(request);
            return View(request);
        }

        // Snapshot of everything about to be wiped and rebuilt by UpdateWithLinesAsync, so the
        // Audit Logs Details modal has a real "before" to show instead of always "N/A".
        var before = new
        {
            Header = await _closings.GetByIdAsync(id),
            Expenses = await _expenses.GetByDailyClosingAsync(id),
            NonCashPayments = await _nonCashPayments.GetByDailyClosingAsync(id),
            Breakdown = await _closings.GetBreakdownAsync(id)
        };

        try
        {
            var closing = await _closings.UpdateWithLinesAsync(id, new UpdateDailyClosingWithLinesRequest
            {
                Date = request.Date,
                MainReading = request.MainReading,
                Note = request.Note,
                GeneralExpenses = request.GeneralExpenses.Select(r => new GeneralExpenseLine
                {
                    ReceiverId = r.ReceiverId,
                    Amount = r.Amount,
                    Note = r.Note
                }).ToList(),
                InvestorExpenses = request.InvestorExpenses.Select(r => new InvestorExpenseLine
                {
                    InvestorId = r.InvestorId,
                    ReceiverId = r.ReceiverId,
                    Amount = r.Amount,
                    Note = r.Note
                }).ToList(),
                Advances = request.Advances.Select(r => new EmployeeLedgerLine
                {
                    EmployeeId = r.EmployeeId,
                    Amount = r.Amount,
                    Note = r.Note
                }).ToList(),
                Deductions = request.Deductions.Select(r => new EmployeeLedgerLine
                {
                    EmployeeId = r.EmployeeId,
                    Amount = r.Amount,
                    Note = r.Note
                }).ToList(),
                Credits = request.Credits.Select(r => new CustomerLedgerLine
                {
                    CustomerId = r.CustomerId,
                    Amount = r.Amount,
                    Note = r.Note
                }).ToList(),
                Cashbacks = request.Cashbacks.Select(r => new CustomerLedgerLine
                {
                    CustomerId = r.CustomerId,
                    Amount = r.Amount,
                    Note = r.Note
                }).ToList(),
                NonCashPayments = request.NonCashPayments.Select(r => new NonCashPaymentLine
                {
                    PaymentMethod = r.PaymentMethod,
                    Amount = r.Amount,
                    Note = r.Note
                }).ToList()
            });

            await _auditLogs.LogAsync("DailyClosing", "Update", id.ToString(), CurrentUserId, CurrentUsername,
                $"Edited daily closing for {closing.Date:yyyy-MM-dd} (header + line items).",
                oldValues: AuditJson(before), newValues: AuditJson(request));

            TempData["Success"] = "Daily closing updated.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateEditDropdownsAsync(request);
            return View(request);
        }
    }

    private async Task PopulateEditDropdownsAsync(EditDailyClosingViewModel vm)
    {
        vm.Receivers = await _receivers.GetAllAsync();
        vm.Investors = await _investors.GetActiveAsync();
        vm.Employees = await _employees.GetActiveAsync();
        vm.Customers = await _customers.GetAllAsync();
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
            $"Added expense {expense.Amount:N2} to daily closing {request.DailyClosingId}.", newValues: AuditJson(expense));

        return RedirectToAction(nameof(Details), new { id = request.DailyClosingId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Supervisor}")]
    public async Task<IActionResult> DeleteExpense(int id, int dailyClosingId)
    {
        var before = (await _expenses.GetByDailyClosingAsync(dailyClosingId)).FirstOrDefault(e => e.Id == id);
        await _expenses.DeleteAsync(id);
        await _auditLogs.LogAsync("Expense", "Delete", id.ToString(), CurrentUserId, CurrentUsername,
            $"Deleted expense from daily closing {dailyClosingId}.", oldValues: AuditJson(before));
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
            $"Added {payment.PaymentMethod} payment {payment.Amount:N2} to daily closing {request.DailyClosingId}.", newValues: AuditJson(payment));

        return RedirectToAction(nameof(Details), new { id = request.DailyClosingId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Supervisor}")]
    public async Task<IActionResult> DeleteNonCashPayment(int id, int dailyClosingId)
    {
        var before = (await _nonCashPayments.GetByDailyClosingAsync(dailyClosingId)).FirstOrDefault(p => p.Id == id);
        await _nonCashPayments.DeleteAsync(id);
        await _auditLogs.LogAsync("NonCashPayment", "Delete", id.ToString(), CurrentUserId, CurrentUsername,
            $"Deleted non-cash payment from daily closing {dailyClosingId}.", oldValues: AuditJson(before));
        return RedirectToAction(nameof(Details), new { id = dailyClosingId });
    }

    // --- Quick-add from inside the Create/Edit form's per-row "+" buttons — see daily-close.js'
    // wireQuickAdd(). Deliberately live here, gated only by this controller's own class-level
    // [RequirePrivilege("DailyClosing")], rather than reusing Employees/Customers/Receivers'
    // CreateJson actions, which each require that entity's own separate privilege — someone doing
    // Daily Closing shouldn't need Employees/Customers/Receivers access too just to add a person
    // they're about to record a line item against. ---

    [HttpPost("/api/daily-closing/quick-add/employee")]
    public async Task<IActionResult> QuickAddEmployee([FromBody] QuickAddEmployeeRequest request)
    {
        try
        {
            var employee = await _employees.CreateAsync(new CreateEmployeeRequest
            {
                Name = request.Name,
                PhoneNumber = request.PhoneNumber,
                Position = request.Position,
                BaseSalary = request.BaseSalary
            });

            await _auditLogs.LogAsync("Employee", "Add", employee.Id.ToString(), CurrentUserId, CurrentUsername,
                $"Created employee '{employee.Name}' from Daily Close.", newValues: AuditJson(employee));
            return Json(new { status = "success", id = employee.Id });
        }
        catch (InvalidOperationException ex)
        {
            return Json(new { status = "error", message = ex.Message });
        }
    }

    [HttpPost("/api/daily-closing/quick-add/customer")]
    public async Task<IActionResult> QuickAddCustomer([FromBody] QuickAddCustomerRequest request)
    {
        var customer = await _customers.CreateAsync(new CreateCustomerRequest { Name = request.Name, PhoneNumber = request.PhoneNumber ?? "" });
        await _auditLogs.LogAsync("Customer", "Add", customer.Id.ToString(), CurrentUserId, CurrentUsername,
            $"Created customer '{customer.Name}' from Daily Close.", newValues: AuditJson(customer));
        return Json(new { status = "success", id = customer.Id });
    }

    [HttpPost("/api/daily-closing/quick-add/receiver")]
    public async Task<IActionResult> QuickAddReceiver([FromBody] QuickAddReceiverRequest request)
    {
        var receiver = await _receivers.CreateAsync(new SaveReceiverRequest { Name = request.Name });
        await _auditLogs.LogAsync("Receiver", "Add", receiver.Id.ToString(), CurrentUserId, CurrentUsername,
            $"Created receiver '{receiver.Name}' from Daily Close.", newValues: AuditJson(receiver));
        return Json(new { status = "success", id = receiver.Id });
    }
}
