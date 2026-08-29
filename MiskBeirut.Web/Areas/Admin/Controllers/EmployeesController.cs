using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiskBeirut.Application.Dtos.Employees;
using MiskBeirut.Application.Managers;
using MiskBeirut.Core.Constants;
using MiskBeirut.Core.Enums;
using MiskBeirut.Web.Areas.Admin.Models.Employees;
using MiskBeirut.Web.Authorization;
using MiskBeirut.Web.Filters;

namespace MiskBeirut.Web.Areas.Admin.Controllers;

// No class-level [RequirePrivilege]: Deductions & Advances is an independently grantable page,
// not implied by the base "Employees" privilege — see CustomersController for the same pattern.
[TypeFilter(typeof(EnsureMonthlyPayrollFilter))]
public class EmployeesController : AdminControllerBase
{
    private readonly EmployeeManager _employees;
    private readonly PayrollManager _payroll;
    private readonly DailyClosingManager _dailyClosings;
    private readonly AuditLogManager _auditLogs;

    public EmployeesController(EmployeeManager employees, PayrollManager payroll, DailyClosingManager dailyClosings, AuditLogManager auditLogs, BackofficePageContentManager pages) : base(pages)
    {
        _employees = employees;
        _payroll = payroll;
        _dailyClosings = dailyClosings;
        _auditLogs = auditLogs;
    }

    private static readonly string[] MonthNames =
    {
        "All Months", "January", "February", "March", "April", "May", "June",
        "July", "August", "September", "October", "November", "December"
    };

    [RequirePrivilege("Employees")]
    public async Task<IActionResult> Index(string? year, string? month, string view_type = "working", string? search = "")
    {
        await LoadPageAsync("Employees");

        var now = DateTime.UtcNow;
        var targetYear = string.IsNullOrEmpty(year) ? now.Year : year != "AllYears" && int.TryParse(year, out var y) ? y : 0;
        var targetMonth = string.IsNullOrEmpty(month) ? now.Month : month != "AllMonths" && int.TryParse(month, out var m) ? m : 0;

        var employees = await _employees.GetActiveAsync();
        if (!string.IsNullOrWhiteSpace(search))
        {
            employees = employees.Where(e => e.Name.Contains(search, StringComparison.OrdinalIgnoreCase) || e.Id.ToString() == search).ToList();
        }

        // TODO: old code auto-created a working record (with carryover debt) for the current month here
        // via ApplyCarryoverDebtAsync — not ported, PayrollManager has no equivalent yet.
        var records = new List<EmployeeWorkingRecordViewModel>();
        foreach (var employee in employees)
        {
            var working = targetYear != 0 && targetMonth != 0
                ? await _payroll.GetAsync(employee.Id, targetYear, targetMonth)
                : null;

            var isWorking = working?.IsWorking ?? false;
            if (view_type == "working" && !isWorking)
                continue;
            if (view_type == "not_working" && isWorking)
                continue;

            records.Add(new EmployeeWorkingRecordViewModel
            {
                Id = working?.Id ?? 0,
                EmployeeId = employee.Id,
                IsWorking = isWorking,
                WorkingDays = working?.WorkingDays ?? 0,
                Employee = new EmployeeShortViewModel
                {
                    Id = employee.Id,
                    Name = employee.Name,
                    PhoneNumber = employee.PhoneNumber,
                    Position = employee.Position,
                    BaseSalary = employee.BaseSalary
                }
            });
        }

        var model = new EmployeesPageViewModel
        {
            Records = records,
            CurrentYear = targetYear,
            CurrentMonth = targetMonth,
            ViewType = view_type,
            Search = search ?? "",
            MonthName = targetMonth is >= 0 and <= 12 ? MonthNames[targetMonth] : "All Months"
        };

        return View(model);
    }

    /// <summary>Control Panel report: every Advance and Deduct ledger entry across all employees.</summary>
    [RequirePrivilege("DeductionsAdvances")]
    public async Task<IActionResult> DeductionsAdvances(int? month, int? year)
    {
        await LoadPageAsync("DeductionsAdvances");

        var advances = await _employees.GetLedgerReportAsync(EmployeeLedgerType.Advance, month, year);
        var deductions = await _employees.GetLedgerReportAsync(EmployeeLedgerType.Deduct, month, year);

        ViewData["CurrentMonth"] = month;
        ViewData["CurrentYear"] = year;

        return View((Advances: advances, Deductions: deductions));
    }

    [RequirePrivilege("Employees")]
    public async Task<IActionResult> Details(int id, int? month, int? year)
    {
        var employee = await _employees.GetByIdAsync(id);
        if (employee is null)
            return NotFound();

        var ledger = (await _employees.GetLedgerAsync(id)).OrderByDescending(l => l.Date).AsEnumerable();
        if (month.HasValue)
            ledger = ledger.Where(l => l.Date.Month == month.Value);
        if (year.HasValue)
            ledger = ledger.Where(l => l.Date.Year == year.Value);

        ViewData["CurrentMonth"] = month;
        ViewData["CurrentYear"] = year;

        var vm = new EmployeeDetailsViewModel
        {
            Employee = employee,
            Ledger = ledger.ToList(),
            WorkingRecords = await _payroll.GetByEmployeeAsync(id),
            RecentClosings = (await _dailyClosings.GetAllAsync()).Take(60).ToList(),
            NewEntry = new AddEmployeeLedgerEntryViewModel { EmployeeId = id }
        };
        return View(vm);
    }

    [HttpGet]
    [RequirePrivilege("Employees")]
    public IActionResult Create() => View(new EmployeeFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePrivilege("Employees")]
    public async Task<IActionResult> Create(EmployeeFormViewModel request)
    {
        if (!ModelState.IsValid)
            return View(request);

        var employee = await _employees.CreateAsync(new CreateEmployeeRequest
        {
            Name = request.Name,
            PhoneNumber = request.PhoneNumber,
            Position = request.Position,
            BaseSalary = request.BaseSalary
        });

        await _auditLogs.LogAsync("Employee", "Add", employee.Id.ToString(), CurrentUserId, CurrentUsername,
            $"Created employee '{employee.Name}'.", newValues: AuditJson(employee));

        TempData["Success"] = $"Employee '{employee.Name}' created.";
        return RedirectToAction(nameof(Details), new { id = employee.Id });
    }

    [HttpGet]
    [RequirePrivilege("Employees")]
    public async Task<IActionResult> Edit(int id)
    {
        var employee = await _employees.GetByIdAsync(id);
        if (employee is null)
            return NotFound();

        return View(new EmployeeFormViewModel
        {
            Id = employee.Id,
            Name = employee.Name,
            PhoneNumber = employee.PhoneNumber,
            Position = employee.Position,
            BaseSalary = employee.BaseSalary,
            IsActive = employee.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePrivilege("Employees")]
    public async Task<IActionResult> Edit(int id, EmployeeFormViewModel request)
    {
        if (id != request.Id)
            return BadRequest();

        if (!ModelState.IsValid)
            return View(request);

        var before = await _employees.GetByIdAsync(id);

        var after = await _employees.UpdateAsync(id, new UpdateEmployeeRequest
        {
            Name = request.Name,
            PhoneNumber = request.PhoneNumber,
            Position = request.Position,
            BaseSalary = request.BaseSalary,
            IsActive = request.IsActive
        });

        await _auditLogs.LogAsync("Employee", "Update", id.ToString(), CurrentUserId, CurrentUsername,
            $"Updated employee '{request.Name}'.", oldValues: AuditJson(before), newValues: AuditJson(after));

        TempData["Success"] = "Employee updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePrivilege("Employees")]
    public async Task<IActionResult> AddLedgerEntry([Bind(Prefix = "NewEntry")] AddEmployeeLedgerEntryViewModel request)
    {
        if (!ModelState.IsValid)
            return RedirectToAction(nameof(Details), new { id = request.EmployeeId });

        try
        {
            var entry = await _employees.AddLedgerEntryAsync(new CreateEmployeeLedgerEntryRequest
            {
                Date = DateOnly.FromDateTime(DateTime.Today),
                Amount = -Math.Abs(request.Amount),
                Type = request.Type,
                Note = request.Note,
                EmployeeId = request.EmployeeId,
                DailyClosingId = request.DailyClosingId
            });

            await _auditLogs.LogAsync("EmployeeLedger", "Add", entry.Id.ToString(), CurrentUserId, CurrentUsername,
                $"Added {entry.Type} {entry.Amount:N2} to employee {request.EmployeeId}.", newValues: AuditJson(entry));

            TempData["Success"] = "Ledger entry added.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id = request.EmployeeId });
    }

    // --- JSON API used by the ported Areas/Admin/Views/Employees/Index.cshtml modals ---

    [HttpGet("/api/employees/{id:int}")]
    [RequirePrivilege("Employees")]
    public async Task<IActionResult> GetJson(int id)
    {
        var employee = await _employees.GetByIdAsync(id);
        if (employee is null)
            return NotFound();

        return Json(new { id = employee.Id, name = employee.Name, phone_number = employee.PhoneNumber, position = employee.Position, base_salary = employee.BaseSalary });
    }

    [HttpPost("/api/employees")]
    [RequirePrivilege("Employees")]
    public async Task<IActionResult> CreateJson([FromBody] EmployeeApiRequest request)
    {
        if (!ModelState.IsValid)
            return Json(new { status = "error", message = FirstModelError() });

        EmployeeDto employee;
        try
        {
            employee = await _employees.CreateAsync(new CreateEmployeeRequest
            {
                Name = request.Name,
                PhoneNumber = request.PhoneNumber,
                Position = request.Position,
                BaseSalary = request.BaseSalary
            });
        }
        catch (InvalidOperationException ex)
        {
            return Json(new { status = "error", message = ex.Message });
        }

        if (request.Year > 0 && request.Month > 0)
        {
            await _payroll.SaveAsync(new SaveEmployeeWorkingRequest
            {
                EmployeeId = employee.Id,
                Year = request.Year,
                Month = request.Month,
                WorkingDays = (int)request.WorkingDays,
                IsWorking = true
            });
        }

        await _auditLogs.LogAsync("Employee", "Add", employee.Id.ToString(), CurrentUserId, CurrentUsername,
            $"Created employee '{employee.Name}'.", newValues: AuditJson(employee));

        return Json(new { status = "success", id = employee.Id });
    }

    [HttpPut("/api/employees/{id:int}")]
    [RequirePrivilege("Employees")]
    public async Task<IActionResult> UpdateJson(int id, [FromBody] EmployeeApiRequest request)
    {
        if (!ModelState.IsValid)
            return Json(new { status = "error", message = FirstModelError() });

        var existing = await _employees.GetByIdAsync(id);
        if (existing is null)
            return NotFound();

        EmployeeDto after;
        try
        {
            after = await _employees.UpdateAsync(id, new UpdateEmployeeRequest
            {
                Name = request.Name,
                PhoneNumber = request.PhoneNumber,
                Position = request.Position,
                BaseSalary = request.BaseSalary,
                IsActive = existing.IsActive
            });
        }
        catch (InvalidOperationException ex)
        {
            return Json(new { status = "error", message = ex.Message });
        }

        await _auditLogs.LogAsync("Employee", "Update", id.ToString(), CurrentUserId, CurrentUsername,
            $"Updated employee '{request.Name}'.", oldValues: AuditJson(existing), newValues: AuditJson(after));

        return Json(new { status = "success" });
    }

    [HttpGet("/api/employees/payroll")]
    [RequirePrivilege("Employees")]
    public async Task<IActionResult> GetPayrollForMonthJson([FromQuery] int year, [FromQuery] int month)
    {
        var records = await _payroll.GetAllForMonthAsync(year, month);
        return Json(records.Select(r => new { r.Id, r.EmployeeId, workingDays = r.WorkingDays ?? 0, isWorking = r.IsWorking }));
    }

    [HttpPost("/api/employees/payroll")]
    [RequirePrivilege("Employees")]
    public async Task<IActionResult> SavePayrollJson([FromBody] SaveEmployeeWorkingRequest request)
    {
        if (request.EmployeeId <= 0)
            return Json(new { status = "error", message = "Missing employee." });

        var before = await _payroll.GetAsync(request.EmployeeId, request.Year, request.Month);
        await _payroll.SaveAsync(request);

        await _auditLogs.LogAsync("EmployeeWorking", "Update", request.EmployeeId.ToString(), CurrentUserId, CurrentUsername,
            $"Set working status for employee {request.EmployeeId} ({request.Year}-{request.Month}) to {(request.IsWorking ? "working" : "not working")}.",
            oldValues: AuditJson(before), newValues: AuditJson(request));

        return Json(new { status = "success" });
    }
}
