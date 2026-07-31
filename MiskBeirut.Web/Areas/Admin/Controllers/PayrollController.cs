using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiskBeirut.Application.Dtos.Employees;
using MiskBeirut.Application.Managers;
using MiskBeirut.Core.Constants;
using MiskBeirut.Web.Areas.Admin.Models.Payroll;

namespace MiskBeirut.Web.Areas.Admin.Controllers;

[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Supervisor}")]
public class PayrollController : AdminControllerBase
{
    private readonly EmployeeManager _employees;
    private readonly PayrollManager _payroll;
    private readonly AuditLogManager _auditLogs;

    public PayrollController(EmployeeManager employees, PayrollManager payroll, AuditLogManager auditLogs)
    {
        _employees = employees;
        _payroll = payroll;
        _auditLogs = auditLogs;
    }

    public async Task<IActionResult> Index()
    {
        var employees = await _employees.GetActiveAsync();
        return View(employees);
    }

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
        await _payroll.SaveAsync(new SaveEmployeeWorkingRequest
        {
            EmployeeId = request.EmployeeId,
            Year = request.Year,
            Month = request.Month,
            Status = request.Status,
            WorkingDays = request.WorkingDays,
            ActualWorkingDays = request.ActualWorkingDays,
            DeductionsTotal = request.DeductionsTotal,
            AdvanceTotal = request.AdvanceTotal,
            ActualSalary = request.ActualSalary,
            Total = request.Total,
            IsWorking = request.IsWorking,
            Note = request.Note
        });

        await _auditLogs.LogAsync("Payroll", "Update", $"{request.EmployeeId}:{request.Year}-{request.Month:00}", CurrentUserId, CurrentUsername,
            $"Saved payroll for employee {request.EmployeeId}, {request.Year}-{request.Month:00}.");

        TempData["Success"] = $"Payroll for {request.Year}-{request.Month:00} saved.";
        return RedirectToAction(nameof(Details), new { employeeId = request.EmployeeId });
    }
}
