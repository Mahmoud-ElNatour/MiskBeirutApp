using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiskBeirut.Application.DTOs;
using MiskBeirut.Application.Services;
using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Interfaces;
using MiskBeirut.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MiskBeirut.Web.Controllers
{
    [Route("[controller]")]
    [Route("api/[controller]")]
    public class EmployeesController : Controller
    {
        private readonly IEmployeeService _employeeService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EmployeesController> _logger;
        private readonly IAuditLogService _auditLogService;

        public EmployeesController(IEmployeeService employeeService, ApplicationDbContext context, ILogger<EmployeesController> logger, IAuditLogService auditLogService)
        {
            _employeeService = employeeService;
            _context = context;
            _logger = logger;
            _auditLogService = auditLogService;
        }

        // GET: /Employees
        [HttpGet]
        [HttpGet("/control-panel/employees")]
        public async Task<IActionResult> Index([FromQuery] string? year, [FromQuery] string? month, [FromQuery] string view_type = "working", [FromQuery] string? search = "")
        {
            _logger.LogInformation("Accessing Employees list. Year: {Year}, Month: {Month}, ViewType: {ViewType}, Search: {Search}", year, month, view_type, search);
            int targetYear = 0;
            int targetMonth = 0;
            var now = DateTime.UtcNow;

            if (string.IsNullOrEmpty(year))
            {
                targetYear = now.Year;
            }
            else if (year != "AllYears" && int.TryParse(year, out var y))
            {
                targetYear = y;
            }

            if (string.IsNullOrEmpty(month))
            {
                targetMonth = now.Month;
            }
            else if (month != "AllMonths" && int.TryParse(month, out var m))
            {
                targetMonth = m;
            }

            // Auto-rollover active employees into the CURRENT month if viewed
            if (targetYear == now.Year && targetMonth == now.Month)
            {
                var activeEmps = await _context.Employees.Where(e => e.IsActive).ToListAsync();
                foreach (var emp in activeEmps)
                {
                    var existing = await _context.EmployeeWorkings
                        .FirstOrDefaultAsync(ew => ew.EmployeeId == emp.Id && ew.Year == targetYear && ew.Month == targetMonth);
                    if (existing == null)
                    {
                        // Calculate if previous month had carryover debt
                        decimal carryoverDebt = await ApplyCarryoverDebtAsync(emp.Id, targetYear, targetMonth);

                        var newRecord = new EmployeeWorking
                        {
                            EmployeeId = emp.Id,
                            Year = targetYear,
                            Month = targetMonth,
                            IsWorking = true,
                            WorkingDays = 0,
                            ActualWorkingDays = 0,
                            DeductionsTotal = carryoverDebt,
                            AdvanceTotal = 0,
                            ActualSalary = -carryoverDebt,
                            Total = 0
                        };
                        _context.EmployeeWorkings.Add(newRecord);
                    }
                }
                await _context.SaveChangesAsync();
            }

            var workingRecords = new List<EmployeeWorking>();

            if (view_type == "working")
            {
                var query = _context.EmployeeWorkings
                    .Include(ew => ew.Employee)
                    .Where(ew => ew.IsWorking);

                if (targetYear != 0) query = query.Where(ew => ew.Year == targetYear);
                if (targetMonth != 0) query = query.Where(ew => ew.Month == targetMonth);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(ew => ew.Employee.Name.Contains(search) || ew.EmployeeId.ToString() == search);
                }

                workingRecords = await query.ToListAsync();
            }
            else if (view_type == "not_working")
            {
                var empQuery = _context.Employees.Where(e => e.IsActive);
                if (!string.IsNullOrWhiteSpace(search))
                {
                    empQuery = empQuery.Where(e => e.Name.Contains(search) || e.Id.ToString() == search);
                }
                var activeEmployees = await empQuery.ToListAsync();

                foreach (var emp in activeEmployees)
                {
                    var wQuery = _context.EmployeeWorkings.Where(ew => ew.EmployeeId == emp.Id);
                    if (targetYear != 0) wQuery = wQuery.Where(ew => ew.Year == targetYear);
                    if (targetMonth != 0) wQuery = wQuery.Where(ew => ew.Month == targetMonth);

                    var record = await wQuery.FirstOrDefaultAsync();
                    if (record == null || !record.IsWorking)
                    {
                        if (record == null)
                        {
                            record = new EmployeeWorking
                            {
                                EmployeeId = emp.Id,
                                Year = targetYear,
                                Month = targetMonth,
                                IsWorking = false,
                                WorkingDays = 0,
                                Employee = emp
                            };
                        }
                        workingRecords.Add(record);
                    }
                }
            }
            else // 'all' (Roster View)
            {
                var empQuery = _context.Employees.Where(e => e.IsActive);
                if (!string.IsNullOrWhiteSpace(search))
                {
                    empQuery = empQuery.Where(e => e.Name.Contains(search) || e.Id.ToString() == search);
                }
                var activeEmployees = await empQuery.ToListAsync();

                foreach (var emp in activeEmployees)
                {
                    var wQuery = _context.EmployeeWorkings.Where(ew => ew.EmployeeId == emp.Id);
                    if (targetYear != 0) wQuery = wQuery.Where(ew => ew.Year == targetYear);
                    if (targetMonth != 0) wQuery = wQuery.Where(ew => ew.Month == targetMonth);

                    var record = await wQuery.FirstOrDefaultAsync();
                    if (record == null)
                    {
                        record = new EmployeeWorking
                        {
                            EmployeeId = emp.Id,
                            Year = targetYear,
                            Month = targetMonth,
                            IsWorking = false,
                            WorkingDays = 0,
                            Employee = emp
                        };
                    }
                    workingRecords.Add(record);
                }
            }

            var monthNames = new[] { "All Months", "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };
            var monthName = targetMonth >= 0 && targetMonth <= 12 ? monthNames[targetMonth] : "All Months";

            var model = new EmployeesPageViewModel
            {
                Records = workingRecords.Select(r => new EmployeeWorkingRecordDto
                {
                    Id = r.Id,
                    EmployeeId = r.EmployeeId,
                    IsWorking = r.IsWorking,
                    WorkingDays = r.WorkingDays,
                    Employee = new EmployeeShortDto
                    {
                        Id = r.Employee.Id,
                        Name = r.Employee.Name,
                        PhoneNumber = r.Employee.PhoneNumber,
                        Position = r.Employee.Position,
                        BaseSalary = r.Employee.BaseSalary
                    }
                }).ToList(),
                CurrentYear = targetYear,
                CurrentMonth = targetMonth,
                ViewType = view_type,
                Search = search ?? "",
                MonthName = monthName
            };

            return View(model);
        }

        // GET: /Employees/5 or /api/Employees/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetEmployee(int id)
        {
            _logger.LogInformation("Retrieving Employee with ID: {ResourceId}", id);
            var emp = await _employeeService.GetEmployeeAsync(id);
            if (emp == null)
            {
                _logger.LogWarning("Employee with ID {ResourceId} was not found.", id);
                return NotFound();
            }
            return Json(emp);
        }

        // POST: /Employees or /api/Employees
        [HttpPost]
        public async Task<IActionResult> CreateEmployee([FromBody] EmployeesDto dto)
        {
            try
            {
                _logger.LogInformation("Creating new Employee with data: {@RequestData}", dto);
                var created = await _employeeService.CreateEmployeeAsync(dto);
                _logger.LogInformation("Successfully created Employee with ID: {ResourceId}", created.Id);

                // Audit log
                try
                {
                    var newValues = new { created.Name, created.PhoneNumber, created.Position, created.BaseSalary, created.IsActive };
                    var username = User?.Identity?.Name ?? "Unknown";
                    var userId = (await _context.Users.FirstOrDefaultAsync(u => u.UserName == username))?.Id;
                    var ipAddress = HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
                    await _auditLogService.LogAddAsync("Employee", created.Id, JsonSerializer.Serialize(newValues), username, userId, ipAddress, $"Employee created: {created.Name}");
                }
                catch (Exception auditEx)
                {
                    _logger.LogError(auditEx, "Failed to create audit log for Employee ID: {ResourceId}", created.Id);
                }

                return Json(new { status = "success", message = "Employee created successfully.", id = created.Id, name = created.Name });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create Employee");
                return Json(new { status = "error", message = ex.Message });
            }
        }

        // PUT: /Employees/5 or /api/Employees/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateEmployee(int id, [FromBody] EmployeesDto dto)
        {
            try
            {
                _logger.LogInformation("Updating Employee ID {ResourceId}", id);
                var oldEmployee = await _context.Employees.FindAsync(id);
                if (oldEmployee == null)
                {
                    _logger.LogWarning("Employee with ID {ResourceId} was not found for update.", id);
                    return NotFound();
                }

                var oldValues = new { oldEmployee.Name, oldEmployee.PhoneNumber, oldEmployee.Position, oldEmployee.BaseSalary, oldEmployee.IsActive };

                var success = await _employeeService.UpdateEmployeeAsync(id, dto);
                if (!success)
                {
                    _logger.LogWarning("Employee with ID {ResourceId} was not found for update.", id);
                    return NotFound();
                }
                _logger.LogInformation("Successfully updated Employee ID {ResourceId}", id);

                // Audit log
                try
                {
                    var newValues = new { dto.Name, dto.PhoneNumber, dto.Position, dto.BaseSalary, dto.IsActive };
                    var username = User?.Identity?.Name ?? "Unknown";
                    var userId = (await _context.Users.FirstOrDefaultAsync(u => u.UserName == username))?.Id;
                    var ipAddress = HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
                    await _auditLogService.LogUpdateAsync("Employee", id, JsonSerializer.Serialize(oldValues), JsonSerializer.Serialize(newValues), username, userId, ipAddress, $"Employee updated: {dto.Name}");
                }
                catch (Exception auditEx)
                {
                    _logger.LogError(auditEx, "Failed to create audit log for Employee ID: {ResourceId}", id);
                }

                return Json(new { status = "success", message = "Employee updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update Employee ID {ResourceId}", id);
                return Json(new { status = "error", message = ex.Message });
            }
        }

        // DELETE: /Employees/5 or /api/Employees/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            try
            {
                _logger.LogWarning("Initiating deletion of Employee ID {ResourceId}", id);
                var employee = await _context.Employees.FindAsync(id);
                if (employee == null)
                {
                    _logger.LogWarning("Employee with ID {ResourceId} was not found for deletion.", id);
                    return NotFound();
                }

                var oldValues = new { employee.Name, employee.PhoneNumber, employee.Position, employee.BaseSalary, employee.IsActive };

                var success = await _employeeService.DeleteEmployeeAsync(id);
                if (!success)
                {
                    _logger.LogWarning("Employee with ID {ResourceId} was not found for deletion.", id);
                    return NotFound();
                }
                _logger.LogInformation("Successfully deleted Employee ID {ResourceId}", id);

                // Audit log
                try
                {
                    var username = User?.Identity?.Name ?? "Unknown";
                    var userId = (await _context.Users.FirstOrDefaultAsync(u => u.UserName == username))?.Id;
                    var ipAddress = HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
                    await _auditLogService.LogDeleteAsync("Employee", id, JsonSerializer.Serialize(oldValues), username, userId, ipAddress, $"Employee deleted: {employee.Name}");
                }
                catch (Exception auditEx)
                {
                    _logger.LogError(auditEx, "Failed to create audit log for Employee ID: {ResourceId}", id);
                }

                return Json(new { status = "success", message = "Employee deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete Employee ID {ResourceId}", id);
                return Json(new { status = "error", message = ex.Message });
            }
        }

        // GET: /Employees/payroll?year=2026&month=6 or /api/Employees/payroll
        [HttpGet("payroll")]
        public async Task<IActionResult> GetPayroll(int year, int month)
        {
            var payroll = await _employeeService.GetPayrollAsync(year, month);
            return Json(payroll);
        }

        // GET: /control-panel/payroll
        [HttpGet("/control-panel/payroll")]
        public async Task<IActionResult> PayrollPage([FromQuery] int? month, [FromQuery] int? year)
        {
            var now = DateTime.UtcNow;
            int selectedMonth = month ?? now.Month;
            int selectedYear = year ?? now.Year;

            var records = await _context.EmployeeWorkings
                .Include(ew => ew.Employee)
                .Where(ew => ew.Year == selectedYear && ew.Month == selectedMonth && ew.IsWorking)
                .ToListAsync();

            ViewData["CurrentMonth"] = selectedMonth;
            ViewData["CurrentYear"] = selectedYear;

            return View("Payroll", records);
        }

        // GET: /control-panel/deductions-advances
        [HttpGet("/control-panel/deductions-advances")]
        public async Task<IActionResult> DeductionsAdvances([FromQuery] int? month, [FromQuery] int? year)
        {
            var now = DateTime.UtcNow;
            int selectedMonth = month ?? now.Month;
            int selectedYear = year ?? now.Year;

            var advancesQuery = _context.Advances.Include(a => a.Employee).AsQueryable();
            var deductionsQuery = _context.Deductions.Include(d => d.Employee).AsQueryable();

            if (selectedYear > 0)
            {
                advancesQuery = advancesQuery.Where(a => a.Date.Year == selectedYear);
                deductionsQuery = deductionsQuery.Where(d => d.Date.Year == selectedYear);
            }
            if (selectedMonth > 0)
            {
                advancesQuery = advancesQuery.Where(a => a.Date.Month == selectedMonth);
                deductionsQuery = deductionsQuery.Where(d => d.Date.Month == selectedMonth);
            }

            var advances = await advancesQuery.OrderByDescending(a => a.Date).ToListAsync();
            var deductions = await deductionsQuery.OrderByDescending(d => d.Date).ToListAsync();

            ViewData["CurrentMonth"] = selectedMonth;
            ViewData["CurrentYear"] = selectedYear;

            return View("DeductionsAdvances", (advances, deductions));
        }

        // GET: /payroll/payslip/5
        [HttpGet("/payroll/payslip/{recordId:int}")]
        public async Task<IActionResult> GeneratePayslipView(int recordId, [FromQuery] bool show_details = false)
        {
            try
            {
                var record = await _context.EmployeeWorkings
                    .Include(ew => ew.Employee)
                    .FirstOrDefaultAsync(ew => ew.Id == recordId);

                if (record == null) return NotFound("Payslip record not found.");

                var advances = new List<Advances>();
                var deductions = new List<Deductions>();

                if (show_details)
                {
                    advances = await _context.Advances
                        .Where(a => a.EmployeeId == record.EmployeeId && a.Date.Year == record.Year && a.Date.Month == record.Month)
                        .ToListAsync();

                    deductions = await _context.Deductions
                        .Where(d => d.EmployeeId == record.EmployeeId && d.Date.Year == record.Year && d.Date.Month == record.Month)
                        .ToListAsync();
                }

                return View("Payslip", (record, show_details, (IEnumerable<Advances>)advances, (IEnumerable<Deductions>)deductions));
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error generating payslip: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /api/employees/5/calculate
        [HttpPost("/api/employees/{recordId:int}/calculate")]
        public async Task<IActionResult> CalculatePayroll(int recordId)
        {
            try
            {
                _logger.LogInformation("Calculating payroll for record ID: {RecordId}", recordId);
                var record = await _context.EmployeeWorkings
                    .Include(ew => ew.Employee)
                    .FirstOrDefaultAsync(ew => ew.Id == recordId);

                if (record == null)
                {
                    _logger.LogWarning("Payroll record ID {RecordId} was not found for calculation.", recordId);
                    return NotFound(new { error = "Payroll record not found" });
                }

                var oldValues = new { record.ActualSalary, record.Total, record.DeductionsTotal, record.AdvanceTotal };

                record.CalculateSalary();
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully calculated payroll for record ID {RecordId}. Salary: {ActualSalary}, Total: {Total}", recordId, record.ActualSalary, record.Total);

                // Audit log
                try
                {
                    var newValues = new { record.ActualSalary, record.Total, record.DeductionsTotal, record.AdvanceTotal };
                    var username = User?.Identity?.Name ?? "Unknown";
                    var userId = (await _context.Users.FirstOrDefaultAsync(u => u.UserName == username))?.Id;
                    var ipAddress = HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
                    await _auditLogService.LogUpdateAsync("EmployeeWorking", recordId, System.Text.Json.JsonSerializer.Serialize(oldValues), System.Text.Json.JsonSerializer.Serialize(newValues), username, userId, ipAddress, $"Payroll calculated for {record.Employee?.Name} ({record.Month}/{record.Year})");
                }
                catch (Exception auditEx)
                {
                    _logger.LogError(auditEx, "Failed to create audit log for payroll record ID: {RecordId}", recordId);
                }

                return Json(new
                {
                    status = "success",
                    message = "Payroll calculated successfully",
                    actual_salary = record.ActualSalary,
                    total = record.Total
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to calculate payroll for record ID {RecordId}", recordId);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // PUT: /api/payroll/{recordId}
        [HttpPut("/api/payroll/{recordId:int}")]
        public async Task<IActionResult> UpdatePayrollRecordAjax(int recordId, [FromBody] System.Text.Json.JsonElement data)
        {
            try
            {
                var record = await _context.EmployeeWorkings
                    .Include(ew => ew.Employee)
                    .FirstOrDefaultAsync(ew => ew.Id == recordId);

                if (record == null) return NotFound(new { error = "Payroll record not found", success = false });

                var oldValues = new { record.ActualWorkingDays, record.DeductionsTotal, record.AdvanceTotal, record.ActualSalary, record.Total };

                if (data.TryGetProperty("actual_working_days", out var awdProp))
                {
                    if (decimal.TryParse(awdProp.ToString(), out var awd))
                    {
                        record.ActualWorkingDays = awd;
                    }
                }

                string? syncDateStr = null;
                if (data.TryGetProperty("date", out var dateProp))
                {
                    syncDateStr = dateProp.GetString();
                }

                if (data.TryGetProperty("deductions_total", out var dedProp))
                {
                    if (decimal.TryParse(dedProp.ToString(), out var newDeductions))
                    {
                        decimal diff = newDeductions - record.DeductionsTotal;
                        if (diff != 0 && !string.IsNullOrEmpty(syncDateStr))
                        {
                            if (DateTime.TryParse(syncDateStr, out var syncDate))
                            {
                                var closing = await _context.DailyClosings
                                    .FirstOrDefaultAsync(dc => dc.Date.Date == syncDate.Date);

                                var newDeduction = new Deductions
                                {
                                    Amount = diff,
                                    EmployeeId = record.EmployeeId,
                                    Note = $"Payroll Adjustment for {record.Month}/{record.Year}",
                                    DailyClosingId = closing?.Id,
                                    Date = syncDate
                                };
                                _context.Deductions.Add(newDeduction);

                                if (closing != null)
                                {
                                    closing.TotalDeductions += diff;
                                    closing.TotalCashout = closing.TotalExpenses + closing.TotalAdvance + closing.TotalDeductions + closing.TotalCredit + closing.TotalCashback;
                                }
                            }
                        }
                        record.DeductionsTotal = newDeductions;
                    }
                }

                if (data.TryGetProperty("advance_total", out var advProp))
                {
                    if (decimal.TryParse(advProp.ToString(), out var newAdvance))
                    {
                        decimal diff = newAdvance - record.AdvanceTotal;
                        if (diff != 0 && !string.IsNullOrEmpty(syncDateStr))
                        {
                            if (DateTime.TryParse(syncDateStr, out var syncDate))
                            {
                                var closing = await _context.DailyClosings
                                    .FirstOrDefaultAsync(dc => dc.Date.Date == syncDate.Date);

                                var newAdvanceRec = new Advances
                                {
                                    Amount = diff,
                                    EmployeeId = record.EmployeeId,
                                    Note = $"Payroll Adjustment for {record.Month}/{record.Year}",
                                    DailyClosingId = closing?.Id,
                                    Date = syncDate
                                };
                                _context.Advances.Add(newAdvanceRec);

                                if (closing != null)
                                {
                                    closing.TotalAdvance += diff;
                                    closing.TotalCashout = closing.TotalExpenses + closing.TotalAdvance + closing.TotalDeductions + closing.TotalCredit + closing.TotalCashback;
                                }
                            }
                        }
                        record.AdvanceTotal = newAdvance;
                    }
                }

                record.CalculateSalary();
                await _context.SaveChangesAsync();

                // Audit log
                try
                {
                    var newValues = new { record.ActualWorkingDays, record.DeductionsTotal, record.AdvanceTotal, record.ActualSalary, record.Total };
                    var username = User?.Identity?.Name ?? "Unknown";
                    var userId = (await _context.Users.FirstOrDefaultAsync(u => u.UserName == username))?.Id;
                    var ipAddress = HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
                    await _auditLogService.LogUpdateAsync("EmployeeWorking", recordId, System.Text.Json.JsonSerializer.Serialize(oldValues), System.Text.Json.JsonSerializer.Serialize(newValues), username, userId, ipAddress, $"Payroll updated for {record.Employee?.Name} ({record.Month}/{record.Year})");
                }
                catch (Exception auditEx)
                {
                    _logger.LogError(auditEx, "Failed to create audit log for payroll record ID: {RecordId}", recordId);
                }

                return Json(new { status = "success", message = "Payroll updated successfully", success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, success = false });
            }
        }

        // GET: /Employees/5/payroll or /api/Employees/5/payroll
        [HttpGet("{employeeId:int}/payroll")]
        public async Task<IActionResult> GetEmployeePayroll(int employeeId)
        {
            var employeePayroll = await _employeeService.GetEmployeePayrollAsync(employeeId);
            return Json(employeePayroll);
        }

        // POST: /Employees/payroll or /api/Employees/payroll
        [HttpPost("payroll")]
        public async Task<IActionResult> UpdatePayrollRecord([FromBody] EmployeeWorkingDto dto)
        {
            try
            {
                var record = await _employeeService.UpdatePayrollRecordAsync(dto);
                return Json(new { status = "success", message = "Payroll updated successfully.", data = record });
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }

        // POST: /Employees/advances or /api/Employees/advances
        [HttpPost("advances")]
        public async Task<IActionResult> CreateAdvance([FromBody] AdvancesDto dto)
        {
            try
            {
                _logger.LogInformation("Creating advance for Employee ID {EmployeeId} with amount: {Amount}", dto.EmployeeId, dto.Amount);
                var created = await _employeeService.CreateAdvanceAsync(dto);
                _logger.LogInformation("Successfully created advance ID {ResourceId} for Employee ID {EmployeeId}", created.Id, dto.EmployeeId);
                return Json(new { status = "success", message = "Advance created successfully.", id = created.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create advance for Employee ID {EmployeeId}", dto.EmployeeId);
                return Json(new { status = "error", message = ex.Message });
            }
        }

        // DELETE: /Employees/advances/5 or /api/Employees/advances/5
        [HttpDelete("advances/{id:int}")]
        public async Task<IActionResult> DeleteAdvance(int id)
        {
            try
            {
                _logger.LogWarning("Initiating deletion of advance ID {ResourceId}", id);
                var success = await _employeeService.DeleteAdvanceAsync(id);
                if (!success)
                {
                    _logger.LogWarning("Advance ID {ResourceId} was not found for deletion.", id);
                    return NotFound();
                }
                _logger.LogInformation("Successfully deleted advance ID {ResourceId}", id);
                return Json(new { status = "success", message = "Advance deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete advance ID {ResourceId}", id);
                return Json(new { status = "error", message = ex.Message });
            }
        }

        // POST: /Employees/deductions or /api/Employees/deductions
        [HttpPost("deductions")]
        public async Task<IActionResult> CreateDeduction([FromBody] DeductionsDto dto)
        {
            try
            {
                _logger.LogInformation("Creating deduction for Employee ID {EmployeeId} with amount: {Amount}", dto.EmployeeId, dto.Amount);
                var created = await _employeeService.CreateDeductionAsync(dto);
                _logger.LogInformation("Successfully created deduction ID {ResourceId} for Employee ID {EmployeeId}", created.Id, dto.EmployeeId);
                return Json(new { status = "success", message = "Deduction created successfully.", id = created.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create deduction for Employee ID {EmployeeId}", dto.EmployeeId);
                return Json(new { status = "error", message = ex.Message });
            }
        }

        // DELETE: /Employees/deductions/5 or /api/Employees/deductions/5
        [HttpDelete("deductions/{id:int}")]
        public async Task<IActionResult> DeleteDeduction(int id)
        {
            try
            {
                _logger.LogWarning("Initiating deletion of deduction ID {ResourceId}", id);
                var success = await _employeeService.DeleteDeductionAsync(id);
                if (!success)
                {
                    _logger.LogWarning("Deduction ID {ResourceId} was not found for deletion.", id);
                    return NotFound();
                }
                _logger.LogInformation("Successfully deleted deduction ID {ResourceId}", id);
                return Json(new { status = "success", message = "Deduction deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete deduction ID {ResourceId}", id);
                return Json(new { status = "error", message = ex.Message });
            }
        }

        // GET: /api/employees/list
        [HttpGet("/api/employees/list")]
        public async Task<IActionResult> GetEmployeesList()
        {
            var list = await _employeeService.GetEmployeesAsync();
            return Json(list.Select(e => new { id = e.Id, name = e.Name }));
        }

        // GET: /api/suggestions/employees
        [HttpGet("/api/suggestions/employees")]
        public async Task<IActionResult> GetEmployeeSuggestions()
        {
            var list = await _employeeService.GetEmployeesAsync();
            return Json(list.Select(e => e.Name).Distinct().ToList());
        }

        #region Helper Methods

        private async Task<decimal> ApplyCarryoverDebtAsync(int employeeId, int currentYear, int currentMonth)
        {
            int prevMonth = currentMonth - 1;
            int prevYear = currentYear;
            if (prevMonth == 0)
            {
                prevMonth = 12;
                prevYear -= 1;
            }

            var prevRecord = await _context.EmployeeWorkings
                .FirstOrDefaultAsync(ew => ew.EmployeeId == employeeId && ew.Year == prevYear && ew.Month == prevMonth);

            decimal carryoverDebt = 0.0m;
            if (prevRecord != null && prevRecord.ActualSalary < 0)
            {
                carryoverDebt = Math.Abs(prevRecord.ActualSalary);
            }

            if (carryoverDebt > 0)
            {
                var existingCarryover = await _context.Deductions
                    .AnyAsync(d => d.EmployeeId == employeeId && d.Date.Year == currentYear && d.Date.Month == currentMonth && d.Note != null && d.Note.Contains("Carryover debt"));

                if (!existingCarryover)
                {
                    var deduction = new Deductions
                    {
                        Amount = carryoverDebt,
                        EmployeeId = employeeId,
                        Date = new DateTime(currentYear, currentMonth, 1),
                        Note = $"Carryover debt from {prevMonth}/{prevYear}"
                    };
                    _context.Deductions.Add(deduction);
                }
            }

            return carryoverDebt;
        }

        #endregion
    }

    #region View Models

    public class EmployeesPageViewModel
    {
        public List<EmployeeWorkingRecordDto> Records { get; set; } = new();
        public int CurrentYear { get; set; }
        public int CurrentMonth { get; set; }
        public string ViewType { get; set; } = "working";
        public string Search { get; set; } = "";
        public string MonthName { get; set; } = "";
    }

    public class EmployeeWorkingRecordDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public bool IsWorking { get; set; }
        public decimal WorkingDays { get; set; }
        public EmployeeShortDto Employee { get; set; } = new();
    }

    public class EmployeeShortDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? PhoneNumber { get; set; }
        public string? Position { get; set; }
        public decimal BaseSalary { get; set; }
    }

    #endregion
}
