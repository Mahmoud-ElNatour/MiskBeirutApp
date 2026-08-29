using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiskBeirut.Application.Dtos.Employees;
using MiskBeirut.Application.Managers;
using MiskBeirut.Core.Enums;
using MiskBeirut.Core.Constants;
using MiskBeirut.Web.Areas.Admin.Models.Payroll;
using MiskBeirut.Web.Authorization;
using MiskBeirut.Web.Filters;

namespace MiskBeirut.Web.Areas.Admin.Controllers;

[RequirePrivilege("Payroll")]
[TypeFilter(typeof(EnsureMonthlyPayrollFilter))]
public class PayrollController : AdminControllerBase
{
    private readonly EmployeeManager _employees;
    private readonly PayrollManager _payroll;
    private readonly AuditLogManager _auditLogs;

    public PayrollController(EmployeeManager employees, PayrollManager payroll, AuditLogManager auditLogs, BackofficePageContentManager pages) : base(pages)
    {
        _employees = employees;
        _payroll = payroll;
        _auditLogs = auditLogs;
    }

    public async Task<IActionResult> Index(int? year, int? month)
    {
        await LoadPageAsync("Payroll");

        var y = year ?? DateTime.UtcNow.Year;
        var m = month ?? DateTime.UtcNow.Month;

        var employees = await _employees.GetActiveAsync();
        var records = await _payroll.GetAllForMonthAsync(y, m);
        var recordsByEmployee = records.ToDictionary(r => r.EmployeeId);

        // GetActiveAsync already excludes employees marked inactive outright; this further excludes
        // anyone whose record for *this specific month* is marked not working (e.g. unpaid leave, or
        // resigned partway through the month) — IsWorking defaults true, so an employee who simply
        // has no record yet for this month still shows up, ready for payroll to be entered.
        var rows = employees
            .Select(e => new PayrollIndexRowViewModel { Employee = e, Record = recordsByEmployee.GetValueOrDefault(e.Id) })
            .Where(r => r.Record is not { IsWorking: false })
            .ToList();

        ViewData["CurrentYear"] = y;
        ViewData["CurrentMonth"] = m;
        return View(rows);
    }

    /// <summary>One employee's full payroll history (every month a record was entered).</summary>
    public async Task<IActionResult> Details(int employeeId)
    {
        var employee = await _employees.GetByIdAsync(employeeId);
        if (employee is null)
            return NotFound();

        return View(new EmployeePayrollViewModel
        {
            Employee = employee,
            Records = await _payroll.GetByEmployeeAsync(employeeId)
        });
    }

    /// <summary>Printable payslip for one employee, one month.</summary>
    public async Task<IActionResult> Payslip(int employeeId, int year, int month)
    {
        var employee = await _employees.GetByIdAsync(employeeId);
        if (employee is null)
            return NotFound();

        var record = await _payroll.GetAsync(employeeId, year, month);
        if (record is null)
            return NotFound();

        var ledger = await _employees.GetLedgerAsync(employeeId);
        var monthLedger = ledger.Where(l => l.Date.Year == year && l.Date.Month == month).ToList();

        return View(new PayslipViewModel
        {
            Employee = employee,
            Record = record,
            Advances = monthLedger.Where(l => l.Type == EmployeeLedgerType.Advance).ToList(),
            Deductions = monthLedger.Where(l => l.Type == EmployeeLedgerType.Deduct).ToList()
        });
    }

    [HttpGet]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> Edit(int employeeId, int year, int month)
    {
        var employee = await _employees.GetByIdAsync(employeeId);
        if (employee is null)
            return NotFound();

        var existing = await _payroll.GetAsync(employeeId, year, month);
        var vm = new PayrollFormViewModel
        {
            EmployeeId = employeeId,
            EmployeeName = employee.Name,
            BaseSalary = existing?.BaseSalary ?? employee.BaseSalary,
            Year = year,
            Month = month,
            Status = existing?.Status,
            WorkingDays = existing?.WorkingDays,
            ActualWorkingDays = existing?.ActualWorkingDays,
            DeductionsTotal = existing?.DeductionsTotal,
            AdvanceTotal = existing?.AdvanceTotal,
            ActualSalary = existing?.ActualSalary,
            Total = existing?.Total,
            IsWorking = existing?.IsWorking ?? true,
            Note = existing?.Note
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> Edit(PayrollFormViewModel request)
    {
        if (!ModelState.IsValid)
            return View(request);

        var before = await _payroll.GetAsync(request.EmployeeId, request.Year, request.Month);

        var after = await _payroll.SaveAsync(new SaveEmployeeWorkingRequest
        {
            EmployeeId = request.EmployeeId,
            Year = request.Year,
            Month = request.Month,
            Status = request.Status,
            WorkingDays = request.WorkingDays,
            ActualWorkingDays = request.ActualWorkingDays,
            DeductionsTotal = request.DeductionsTotal,
            AdvanceTotal = request.AdvanceTotal,
            BaseSalary = request.BaseSalary,
            IsWorking = request.IsWorking,
            Note = request.Note
        });

        await _auditLogs.LogAsync("Payroll", "Update", $"{request.EmployeeId}:{request.Year}-{request.Month:00}", CurrentUserId, CurrentUsername,
            $"Saved payroll for employee {request.EmployeeId}, {request.Year}-{request.Month:00}.",
            oldValues: AuditJson(before), newValues: AuditJson(after));

        TempData["Success"] = $"Payroll for {request.Year}-{request.Month:00} saved.";
        return RedirectToAction(nameof(Details), new { employeeId = request.EmployeeId });
    }
}
