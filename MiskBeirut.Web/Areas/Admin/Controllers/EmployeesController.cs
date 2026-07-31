using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiskBeirut.Application.Dtos.Employees;
using MiskBeirut.Application.Managers;
using MiskBeirut.Core.Constants;
using MiskBeirut.Web.Areas.Admin.Models.Employees;

namespace MiskBeirut.Web.Areas.Admin.Controllers;

[Authorize(Roles = RoleNames.Admin)]
public class EmployeesController : AdminControllerBase
{
    private readonly EmployeeManager _employees;
    private readonly PayrollManager _payroll;
    private readonly DailyClosingManager _dailyClosings;
    private readonly AuditLogManager _auditLogs;

    public EmployeesController(EmployeeManager employees, PayrollManager payroll, DailyClosingManager dailyClosings, AuditLogManager auditLogs)
    {
        _employees = employees;
        _payroll = payroll;
        _dailyClosings = dailyClosings;
        _auditLogs = auditLogs;
    }

    public async Task<IActionResult> Index()
    {
        var employees = await _employees.GetAllAsync();
        return View(employees);
    }

    public async Task<IActionResult> Details(int id)
    {
        var employee = await _employees.GetByIdAsync(id);
        if (employee is null)
            return NotFound();

        var vm = new EmployeeDetailsViewModel
        {
            Employee = employee,
            Ledger = (await _employees.GetLedgerAsync(id)).OrderByDescending(l => l.Date).ToList(),
            WorkingRecords = await _payroll.GetByEmployeeAsync(id),
            RecentClosings = (await _dailyClosings.GetAllAsync()).Take(60).ToList(),
            NewEntry = new AddEmployeeLedgerEntryViewModel { EmployeeId = id }
        };
        return View(vm);
    }

    [HttpGet]
    public IActionResult Create() => View(new EmployeeFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
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

        await _auditLogs.LogAsync("Employee", "Add", employee.Id.ToString(), CurrentUserId, CurrentUsername, $"Created employee '{employee.Name}'.");

        TempData["Success"] = $"Employee '{employee.Name}' created.";
        return RedirectToAction(nameof(Details), new { id = employee.Id });
    }

    [HttpGet]
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
    public async Task<IActionResult> Edit(int id, EmployeeFormViewModel request)
    {
        if (id != request.Id)
            return BadRequest();

        if (!ModelState.IsValid)
            return View(request);

        await _employees.UpdateAsync(id, new UpdateEmployeeRequest
        {
            Name = request.Name,
            PhoneNumber = request.PhoneNumber,
            Position = request.Position,
            BaseSalary = request.BaseSalary,
            IsActive = request.IsActive
        });

        await _auditLogs.LogAsync("Employee", "Update", id.ToString(), CurrentUserId, CurrentUsername, $"Updated employee '{request.Name}'.");

        TempData["Success"] = "Employee updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
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
                $"Added {entry.Type} {entry.Amount:N2} to employee {request.EmployeeId}.");

            TempData["Success"] = "Ledger entry added.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id = request.EmployeeId });
    }
}
