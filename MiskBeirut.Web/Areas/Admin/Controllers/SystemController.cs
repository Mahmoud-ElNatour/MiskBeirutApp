using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiskBeirut.Application.Managers;
using MiskBeirut.Core.Constants;
using MiskBeirut.Core.Entities;
using MiskBeirut.Web.Areas.Admin.Models.Reports;
using MiskBeirut.Web.Areas.Admin.Models.Roles;
using MiskBeirut.Web.Authorization;

namespace MiskBeirut.Web.Areas.Admin.Controllers;

// TODO: port real data wiring from Areas/Admin/_Legacy/Controllers/SystemController.cs (uses old DTOs/entities).
public class SystemController : AdminControllerBase
{
    private readonly RoleManager<IdentityRole<int>> _roleManager;
    private readonly UserManager<User> _userManager;
    private readonly PrivilegeManager _privileges;
    private readonly AuditLogManager _auditLogs;
    private readonly DailyClosingManager _dailyClosings;
    private readonly ExpenseManager _expenses;
    private readonly CustomerManager _customers;
    private readonly EmployeeManager _employees;
    private readonly PayrollManager _payroll;
    private readonly InvestorManager _investors;

    public SystemController(
        RoleManager<IdentityRole<int>> roleManager,
        UserManager<User> userManager,
        PrivilegeManager privileges,
        AuditLogManager auditLogs,
        DailyClosingManager dailyClosings,
        ExpenseManager expenses,
        CustomerManager customers,
        EmployeeManager employees,
        PayrollManager payroll,
        InvestorManager investors,
        BackofficePageContentManager pages) : base(pages)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _privileges = privileges;
        _auditLogs = auditLogs;
        _dailyClosings = dailyClosings;
        _expenses = expenses;
        _customers = customers;
        _employees = employees;
        _payroll = payroll;
        _investors = investors;
    }

    public async Task<IActionResult> ControlPanel()
    {
        await LoadPageAsync("ControlPanel");
        return View();
    }

    [RequirePrivilege("Reports")]
    public async Task<IActionResult> Reports()
    {
        await LoadPageAsync("Reports");

        var closings = await _dailyClosings.GetAllAsync();
        var expenses = await _expenses.GetReportAsync(null, null, null);
        var customers = await _customers.GetAllAsync();
        var employees = await _employees.GetAllAsync();

        return View(new ReportsViewModel
        {
            TotalDailyClosings = closings.Count,
            TotalExpenses = expenses.Count,
            TotalCustomers = customers.Count,
            TotalEmployees = employees.Count
        });
    }

    /// <summary>Sales Report form on the Reports page — one month's closings plus the running customer-balance total.</summary>
    [HttpPost("/api/reports/sales")]
    [RequirePrivilege("Reports")]
    public async Task<IActionResult> SalesReport([FromBody] ReportRequestDto request)
    {
        var closings = await _dailyClosings.GetAllAsync(request.Year, request.Month);
        var totalCustomerBalance = (await _customers.GetAllAsync()).Sum(c => c.Balance);

        return Json(new
        {
            status = "success",
            data = new
            {
                month = request.Month,
                year = request.Year,
                total_sales = closings.Sum(c => c.MainReading),
                // The real cash-drawer reconciliation figure (AdjustedReading minus expenses/advances/
                // non-cash — see DailyClosingManager.ApplyComputedTotals), not the legacy report's
                // "MainReading + customer balances" formula, which didn't correspond to actual cash.
                actual_cash = closings.Sum(c => c.ActualCash ?? 0),
                total_customer_balance = totalCustomerBalance,
                sales = closings.OrderBy(c => c.Date).Select(c => new
                {
                    date = c.Date.ToString("yyyy-MM-dd"),
                    main_reading = c.MainReading,
                    amount = c.AdjustedReading ?? 0
                })
            }
        });
    }

    /// <summary>Payroll Report form on the Reports page — one month's working records across every employee.</summary>
    [HttpPost("/api/reports/payroll")]
    [RequirePrivilege("Reports")]
    public async Task<IActionResult> PayrollReport([FromBody] ReportRequestDto request)
    {
        // IsWorking is per-month — an employee on unpaid leave or who resigned partway through this
        // specific month is excluded here even though they may be Employee.IsActive overall, same
        // definition of "currently working" PayrollController.Index already applies.
        var workings = (await _payroll.GetAllForMonthAsync(request.Year, request.Month))
            .Where(w => w.IsWorking)
            .ToList();
        var employeesById = (await _employees.GetAllAsync()).ToDictionary(e => e.Id);

        return Json(new
        {
            status = "success",
            data = new
            {
                month = request.Month,
                year = request.Year,
                total_payroll = workings.Sum(w => w.Total ?? 0),
                total_employees = workings.Count,
                total_deductions = workings.Sum(w => w.DeductionsTotal ?? 0),
                employees = workings.Select(w =>
                {
                    employeesById.TryGetValue(w.EmployeeId, out var employee);
                    return new
                    {
                        name = employee?.Name ?? "Unknown",
                        position = employee?.Position,
                        // The record's own snapshotted rate (see EmployeeWorking.BaseSalary), not the
                        // employee's current one — a later raise shouldn't rewrite this month's report.
                        base_salary = w.BaseSalary,
                        advance = w.AdvanceTotal ?? 0,
                        deductions = w.DeductionsTotal ?? 0,
                        actual_salary = w.ActualSalary ?? 0,
                        total = w.Total ?? 0
                    };
                })
            }
        });
    }

    /// <summary>
    /// Expenses Report form on the Reports page — general Expenses plus Investor Expenses (money paid
    /// out of investor capital) for one month, each with its own receiver breakdown. The legacy report
    /// had a third, entirely separate "Samer Expenses" book (its own table/receivers) that was never
    /// carried into this domain model — investor expenses are its replacement here.
    /// </summary>
    [HttpPost("/api/reports/expenses")]
    [RequirePrivilege("Reports")]
    public async Task<IActionResult> ExpensesReport([FromBody] ReportRequestDto request)
    {
        var generalExpenses = await _expenses.GetReportAsync(request.Month, request.Year, null);
        var investorExpenses = await _investors.GetExpenseReportAsync(request.Month, request.Year);

        var receiverBreakdown = generalExpenses
            .GroupBy(e => e.ReceiverName)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));

        // One section per active investor — every one of them, not just those who spent something
        // this month — each with its own total and its own receiver-level breakdown (empty if they
        // had no expenses this period).
        var activeInvestors = await _investors.GetActiveAsync();
        var investorExpensesByInvestor = investorExpenses.GroupBy(e => e.InvestorName ?? "Unassigned").ToDictionary(g => g.Key, g => g.ToList());

        var investorBreakdown = activeInvestors
            .Select(investor =>
            {
                var expenses = investorExpensesByInvestor.GetValueOrDefault(investor.Name, []);
                return new
                {
                    investor_name = investor.Name,
                    total = expenses.Sum(e => e.Amount),
                    receivers = expenses.GroupBy(e => e.ReceiverName ?? "Unassigned").ToDictionary(rg => rg.Key, rg => rg.Sum(e => e.Amount))
                };
            })
            .OrderByDescending(x => x.total)
            .ToList();

        var allExpenses = generalExpenses
            .Select(e => new { type = "General", date = e.Date.ToString("yyyy-MM-dd"), receiver = e.ReceiverName, note = e.Note ?? "", amount = e.Amount })
            .Concat(investorExpenses.Select(e => new { type = "Investor", date = e.Date.ToString("yyyy-MM-dd"), receiver = e.ReceiverName ?? "Unassigned", note = e.Note ?? "", amount = e.Amount }))
            .OrderByDescending(e => e.date);

        return Json(new
        {
            status = "success",
            data = new
            {
                month = request.Month,
                year = request.Year,
                total_expenses = generalExpenses.Sum(e => e.Amount) + investorExpenses.Sum(e => e.Amount),
                total_receivers = generalExpenses.Select(e => e.ReceiverId).Distinct().Count(),
                receiver_breakdown = receiverBreakdown,
                investor_breakdown = investorBreakdown,
                expenses = allExpenses
            }
        });
    }

    // --- CSV exports: the Reports page's "Export" button, one per report type. Each mirrors the
    // matching on-screen report's own numbers (real ActualCash, investor expenses in place of the
    // legacy's unported "Samer" book, per-record BaseSalary snapshot, IsWorking-filtered payroll) so
    // what gets downloaded always agrees with what's on screen. ---

    [HttpGet("/api/exports/sales")]
    [RequirePrivilege("Reports")]
    public async Task<IActionResult> ExportSalesCsv([FromQuery] int? month, [FromQuery] int? year)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var y = year ?? today.Year;
        var m = month ?? today.Month;

        var closings = await _dailyClosings.GetAllAsync(y, m);
        var summaries = new List<Application.Dtos.DailyClosings.DailyClosingSummaryDto>();
        foreach (var closing in closings.OrderBy(c => c.Date))
        {
            var summary = await _dailyClosings.GetSummaryAsync(closing.Id);
            if (summary is not null)
                summaries.Add(summary);
        }

        var headers = new[] { "Date", "Main Reading", "Adjusted Reading", "General Expenses", "Employee Advances", "Customer Credits", "Customer Cashbacks", "Actual Cash" };
        var rows = summaries.Select(s => new[]
        {
            s.Date.ToString("yyyy-MM-dd"),
            s.MainReading.ToString("F2"),
            (s.AdjustedReading ?? 0).ToString("F2"),
            s.TotalExpenses.ToString("F2"),
            s.TotalEmployeeAdvances.ToString("F2"),
            Math.Abs(s.TotalCustomerCredits).ToString("F2"),
            s.TotalCustomerCashbacks.ToString("F2"),
            (s.ActualCash ?? 0).ToString("F2")
        }).ToList();

        var totalCustomerBalance = (await _customers.GetAllAsync()).Sum(c => c.Balance);
        rows.Add([]);
        rows.Add(["--- SUMMARY ---", "", "", "", "", "", "", ""]);
        rows.Add(["Total Main Reading", summaries.Sum(s => s.MainReading).ToString("F2"), "", "", "", "", "", ""]);
        rows.Add(["Total Customer Balance", totalCustomerBalance.ToString("F2"), "", "", "", "", "", ""]);
        rows.Add(["Total Actual Cash", summaries.Sum(s => s.ActualCash ?? 0).ToString("F2"), "", "", "", "", "", ""]);

        return CsvFile(headers, rows, $"sales_export_{y}_{m:00}.csv");
    }

    [HttpGet("/api/exports/payroll")]
    [RequirePrivilege("Reports")]
    public async Task<IActionResult> ExportPayrollCsv([FromQuery] int? month, [FromQuery] int? year)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var y = year ?? today.Year;
        var m = month ?? today.Month;

        var workings = (await _payroll.GetAllForMonthAsync(y, m)).Where(w => w.IsWorking).ToList();
        var employeesById = (await _employees.GetAllAsync()).ToDictionary(e => e.Id);

        var headers = new[] { "Employee", "Position", "Base Salary", "Working Days", "Actual Working Days", "Advance", "Deductions", "Actual Salary", "Total" };
        var rows = workings.Select(w =>
        {
            employeesById.TryGetValue(w.EmployeeId, out var employee);
            return new[]
            {
                employee?.Name ?? "Unknown",
                employee?.Position ?? "N/A",
                w.BaseSalary.ToString("F2"),
                (w.WorkingDays ?? 0).ToString(),
                (w.ActualWorkingDays ?? 0).ToString(),
                (w.AdvanceTotal ?? 0).ToString("F2"),
                (w.DeductionsTotal ?? 0).ToString("F2"),
                (w.ActualSalary ?? 0).ToString("F2"),
                (w.Total ?? 0).ToString("F2")
            };
        }).ToList();

        rows.Add([]);
        rows.Add([
            "TOTAL", "", workings.Sum(w => w.BaseSalary).ToString("F2"), "", "",
            workings.Sum(w => w.AdvanceTotal ?? 0).ToString("F2"),
            workings.Sum(w => w.DeductionsTotal ?? 0).ToString("F2"),
            workings.Sum(w => w.ActualSalary ?? 0).ToString("F2"),
            workings.Sum(w => w.Total ?? 0).ToString("F2")
        ]);

        return CsvFile(headers, rows, $"payroll_export_{y}_{m:00}.csv");
    }

    [HttpGet("/api/exports/reports")]
    [RequirePrivilege("Reports")]
    public async Task<IActionResult> ExportExpensesCsv([FromQuery] int? month, [FromQuery] int? year)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var y = year ?? today.Year;
        var m = month ?? today.Month;

        var generalExpenses = await _expenses.GetReportAsync(m, y, null);
        var investorExpenses = await _investors.GetExpenseReportAsync(m, y);

        var headers = new[] { "Type", "Name", $"Total ({m:00}/{y})", "All-Time Total (Receiver)" };
        var rows = new List<string[]> { new[] { "--- GENERAL EXPENSES ---", "", "", "" } };

        foreach (var group in generalExpenses.GroupBy(e => new { e.ReceiverId, e.ReceiverName }).OrderByDescending(g => g.Sum(e => e.Amount)))
        {
            var allTime = await _expenses.GetTotalPaidByReceiverAsync(group.Key.ReceiverId);
            rows.Add(["General", group.Key.ReceiverName, group.Sum(e => e.Amount).ToString("F2"), allTime.ToString("F2")]);
        }

        rows.Add([]);
        rows.Add(["--- INVESTOR EXPENSES ---", "", "", ""]);
        // Every active investor gets a line, not just ones who spent something this period.
        var activeInvestors = await _investors.GetActiveAsync();
        var investorExpensesByInvestor = investorExpenses.GroupBy(e => e.InvestorName ?? "Unassigned").ToDictionary(g => g.Key, g => g.ToList());
        foreach (var investor in activeInvestors.OrderByDescending(inv => investorExpensesByInvestor.GetValueOrDefault(inv.Name, []).Sum(e => e.Amount)))
        {
            var expenses = investorExpensesByInvestor.GetValueOrDefault(investor.Name, []);
            rows.Add([$"Investor: {investor.Name}", "", expenses.Sum(e => e.Amount).ToString("F2"), ""]);
            foreach (var receiverGroup in expenses.GroupBy(e => e.ReceiverName ?? "Unassigned").OrderByDescending(g => g.Sum(e => e.Amount)))
                rows.Add(["", receiverGroup.Key, receiverGroup.Sum(e => e.Amount).ToString("F2"), ""]);
        }

        var totalGeneral = generalExpenses.Sum(e => e.Amount);
        var totalInvestor = investorExpenses.Sum(e => e.Amount);
        rows.Add([]);
        rows.Add(["--- SUMMARY ---", "", "", ""]);
        rows.Add(["General Expenses Total", "", totalGeneral.ToString("F2"), ""]);
        rows.Add(["Investor Expenses Total", "", totalInvestor.ToString("F2"), ""]);
        rows.Add(["GRAND TOTAL", "", (totalGeneral + totalInvestor).ToString("F2"), ""]);

        return CsvFile(headers, rows, $"expenses_export_{y}_{m:00}.csv");
    }

    /// <summary>UTF-8-with-BOM CSV download so Excel renders it correctly instead of guessing ANSI and mangling names — same approach as WebsiteLeadsController.Export.</summary>
    private FileContentResult CsvFile(IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string>> rows, string fileName)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(",", headers.Select(EscapeCsv)));
        foreach (var row in rows)
            builder.AppendLine(string.Join(",", row.Select(EscapeCsv)));

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(builder.ToString())).ToArray();
        return File(bytes, "text/csv", fileName);
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    // --- Role Manager: dynamic roles + page/section privilege assignment ---

    [RequirePrivilege("RoleManager")]
    public async Task<IActionResult> Roles()
    {
        await LoadPageAsync("RoleManager");

        var roles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
        var items = new List<RoleListItemViewModel>();
        foreach (var role in roles)
        {
            var users = await _userManager.GetUsersInRoleAsync(role.Name!);
            items.Add(new RoleListItemViewModel { Id = role.Id, Name = role.Name!, UserCount = users.Count });
        }

        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePrivilege("RoleManager")]
    public async Task<IActionResult> CreateRole(RoleFormViewModel request)
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(request.Name))
        {
            TempData["Error"] = "Role name is required.";
            return RedirectToAction(nameof(Roles));
        }

        if (await _roleManager.RoleExistsAsync(request.Name))
        {
            TempData["Error"] = $"A role named '{request.Name}' already exists.";
            return RedirectToAction(nameof(Roles));
        }

        var result = await _roleManager.CreateAsync(new IdentityRole<int>(request.Name.Trim()));
        if (!result.Succeeded)
        {
            TempData["Error"] = string.Join(" ", result.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Roles));
        }

        await _auditLogs.LogAsync("Role", "Add", request.Name, CurrentUserId, CurrentUsername,
            $"Created role '{request.Name}'.", newValues: AuditJson(new { request.Name }));
        TempData["Success"] = $"Role '{request.Name}' created. Assign it privileges before assigning it to users.";
        return RedirectToAction(nameof(Roles));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePrivilege("RoleManager")]
    public async Task<IActionResult> DeleteRole(int id)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role is null)
            return NotFound();

        if (role.Name == RoleNames.Admin)
        {
            TempData["Error"] = "The Admin role cannot be deleted — it's the system's safety net and always retains full access.";
            return RedirectToAction(nameof(Roles));
        }

        var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
        if (usersInRole.Count > 0)
        {
            TempData["Error"] = $"Cannot delete '{role.Name}': it is assigned to {usersInRole.Count} user(s). Reassign them first.";
            return RedirectToAction(nameof(Roles));
        }

        var grantedPrivilegeIds = await _privileges.GetGrantedPrivilegeIdsAsync(id);

        await _privileges.DeleteRolePrivilegesAsync(role.Id);
        await _roleManager.DeleteAsync(role);

        await _auditLogs.LogAsync("Role", "Delete", role.Name ?? id.ToString(), CurrentUserId, CurrentUsername,
            $"Deleted role '{role.Name}'.", oldValues: AuditJson(new { role.Id, role.Name, PrivilegeIds = grantedPrivilegeIds }));
        TempData["Success"] = $"Role '{role.Name}' deleted.";
        return RedirectToAction(nameof(Roles));
    }

    [RequirePrivilege("RoleManager")]
    public async Task<IActionResult> RolePrivileges(int id)
    {
        await LoadPageAsync("RoleManager");

        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role is null)
            return NotFound();

        var allPrivileges = await _privileges.GetAllAsync();
        var grantedIds = (await _privileges.GetGrantedPrivilegeIdsAsync(id)).ToHashSet();

        var vm = new RolePrivilegesViewModel
        {
            RoleId = id,
            RoleName = role.Name ?? "",
            IsAdminRole = role.Name == RoleNames.Admin,
            Sections = allPrivileges
                .Where(p => p.IsSection)
                .OrderBy(p => p.Name)
                .Select(section => new RoleSectionPrivilegesViewModel
                {
                    SectionPrivilegeId = section.Id,
                    SectionName = section.Name,
                    SectionGranted = grantedIds.Contains(section.Id),
                    Pages = allPrivileges
                        .Where(p => !p.IsSection && p.SectionKey == section.Key)
                        .OrderBy(p => p.Name)
                        .Select(page => new RolePagePrivilegeViewModel
                        {
                            PrivilegeId = page.Id,
                            Name = page.Name,
                            Granted = grantedIds.Contains(page.Id)
                        }).ToList()
                }).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePrivilege("RoleManager")]
    public async Task<IActionResult> SaveRolePrivileges(int roleId, [FromForm] List<int> privilegeIds)
    {
        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        if (role is null)
            return NotFound();

        var before = await _privileges.GetGrantedPrivilegeIdsAsync(roleId);

        await _privileges.SetRolePrivilegesAsync(roleId, privilegeIds ?? []);

        await _auditLogs.LogAsync("Role", "Update", role.Name ?? roleId.ToString(), CurrentUserId, CurrentUsername,
            $"Updated privileges for role '{role.Name}' ({(privilegeIds ?? []).Count} granted).",
            oldValues: AuditJson(new { PrivilegeIds = before }), newValues: AuditJson(new { PrivilegeIds = privilegeIds ?? [] }));

        TempData["Success"] = $"Privileges for '{role.Name}' saved.";
        return RedirectToAction(nameof(Roles));
    }

    // --- Role Manager: which users hold this role — the same membership Users/Edit's per-user
    // checklist manages, just edited from the role's side instead of the user's. ---

    [RequirePrivilege("RoleManager")]
    public async Task<IActionResult> RoleUsers(int id)
    {
        await LoadPageAsync("RoleManager");

        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role is null)
            return NotFound();

        var usersInRole = (await _userManager.GetUsersInRoleAsync(role.Name!)).Select(u => u.Id).ToHashSet();
        var allUsers = _userManager.Users.OrderBy(u => u.UserName).ToList();

        var vm = new RoleUsersViewModel
        {
            RoleId = id,
            RoleName = role.Name ?? "",
            IsAdminRole = role.Name == RoleNames.Admin,
            Users = allUsers.Select(u => new RoleUserItemViewModel
            {
                UserId = u.Id,
                Username = u.UserName ?? "",
                Email = u.Email,
                Assigned = usersInRole.Contains(u.Id)
            }).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePrivilege("RoleManager")]
    public async Task<IActionResult> SaveRoleUsers(int roleId, [FromForm] List<int> userIds)
    {
        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        if (role is null)
            return NotFound();

        var roleName = role.Name!;
        var requestedIds = (userIds ?? []).ToHashSet();
        var currentUsers = await _userManager.GetUsersInRoleAsync(roleName);
        var currentIds = currentUsers.Select(u => u.Id).ToHashSet();

        var toRemoveIds = currentIds.Except(requestedIds).ToList();
        if (roleName == RoleNames.Admin && toRemoveIds.Count > 0)
        {
            var remainingAdmins = currentUsers.Count - toRemoveIds.Count;
            if (remainingAdmins < 1)
            {
                TempData["Error"] = "Cannot remove every user from the Admin role — at least one must remain.";
                return RedirectToAction(nameof(RoleUsers), new { id = roleId });
            }
        }

        foreach (var userId in requestedIds.Except(currentIds))
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is not null)
                await _userManager.AddToRoleAsync(user, roleName);
        }

        foreach (var userId in toRemoveIds)
        {
            var user = currentUsers.FirstOrDefault(u => u.Id == userId);
            if (user is not null)
                await _userManager.RemoveFromRoleAsync(user, roleName);
        }

        await _auditLogs.LogAsync("Role", "Update", roleName, CurrentUserId, CurrentUsername,
            $"Updated user membership for role '{roleName}' ({requestedIds.Count} assigned).",
            oldValues: AuditJson(new { UserIds = currentIds }), newValues: AuditJson(new { UserIds = requestedIds }));

        TempData["Success"] = $"Users for '{roleName}' saved.";
        return RedirectToAction(nameof(Roles));
    }
}
