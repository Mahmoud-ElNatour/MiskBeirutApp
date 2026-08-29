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
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace MiskBeirut.Web.Controllers
{
    [Route("[controller]")]
    [Route("api/[controller]")]
    [Authorize]
    public class DailyClosingController : Controller
    {
        private readonly IDailyClosingService _dailyClosingService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DailyClosingController> _logger;
        private readonly IAuditLogService _auditLogService;

        public DailyClosingController(IDailyClosingService dailyClosingService, ApplicationDbContext context, ILogger<DailyClosingController> logger, IAuditLogService auditLogService)
        {
            _dailyClosingService = dailyClosingService;
            _context = context;
            _logger = logger;
            _auditLogService = auditLogService;
        }

        // GET: /api/admin-password
        [HttpGet("/api/admin-password")]
        public async Task<IActionResult> GetAdminPasswordHash()
        {
            var setting = await _context.SiteSettings.FirstOrDefaultAsync(s => s.Key == "admin_password_hash");
            var hash = setting?.Value ?? "8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918";
            return Json(new { status = "success", password = hash });
        }

        // GET: /control-panel/sales
        [HttpGet("/control-panel/sales")]
        public async Task<IActionResult> SalesPage([FromQuery] int? month, [FromQuery] int? year)
        {
            var now = DateTime.UtcNow;
            int selectedMonth = month ?? now.Month;
            int selectedYear = year ?? now.Year;

            var closings = await _context.DailyClosings
                .Where(c => c.Date.Year == selectedYear && c.Date.Month == selectedMonth)
                .OrderByDescending(c => c.Date)
                .ToListAsync();

            decimal totalSalesSum = closings.Sum(c => c.AdjustedReading);
            decimal totalExpensesSum = closings.Sum(c => c.TotalExpenses);
            decimal totalActualCashSum = closings.Sum(c => c.ActualCash);
            decimal totalCreditSum = closings.Sum(c => c.TotalCredit);
            decimal totalFivePercentSum = closings.Sum(c => c.FivePercent);

            var monthNames = new[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };
            string monthName = selectedMonth >= 1 && selectedMonth <= 12 ? monthNames[selectedMonth - 1] : "N/A";

            ViewData["CurrentMonth"] = selectedMonth;
            ViewData["CurrentYear"] = selectedYear;
            ViewData["MonthName"] = monthName;
            ViewData["TotalSalesSum"] = totalSalesSum;
            ViewData["TotalExpensesSum"] = totalExpensesSum;
            ViewData["TotalActualCashSum"] = totalActualCashSum;
            ViewData["TotalCreditSum"] = totalCreditSum;
            ViewData["TotalFivePercentSum"] = totalFivePercentSum;

            return View("Sales", closings);
        }

        // GET: /api/daily-close/5
        [HttpGet("/api/daily-close/{id:int}")]
        public async Task<IActionResult> GetDailyClosingDetails(int id)
        {
            var closing = await _context.DailyClosings
                .Include(c => c.Expenses).ThenInclude(e => e.Receiver)
                .Include(c => c.AhmadMistrahExpenses).ThenInclude(e => e.Receiver)
                .Include(c => c.SamerExpenses).ThenInclude(e => e.Receiver)
                .Include(c => c.Advances).ThenInclude(a => a.Employee)
                .Include(c => c.Deductions).ThenInclude(d => d.Employee)
                .Include(c => c.Credits).ThenInclude(c => c.Customer)
                .Include(c => c.Cashbacks).ThenInclude(c => c.Customer)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (closing == null) return NotFound(new { error = "Failed to fetch details" });

            return Json(new {
                id = closing.Id,
                date = closing.Date.ToString("yyyy-MM-dd"),
                main_reading = closing.MainReading,
                dr_smashed = closing.DrSmashed,
                adjusted_reading = closing.AdjustedReading,
                total_expenses = closing.TotalExpenses,
                total_advance = closing.TotalAdvance,
                total_credit = closing.TotalCredit,
                total_cashback = closing.TotalCashback,
                total_deductions = closing.TotalDeductions,
                five_percent = closing.FivePercent,
                total_cashout = closing.TotalCashout,
                actual_cash = closing.ActualCash,
                expenses = closing.Expenses.Select(e => new { amount = e.Amount, note = e.Note, receiver_id = e.ReceiverId, receiver = e.Receiver?.Name ?? "Unassigned" }),
                ahmad_expenses = closing.AhmadMistrahExpenses.Select(e => new { amount = e.Amount, note = e.Note, receiver_id = e.ReceiverId, receiver = e.Receiver?.Name ?? "Unassigned" }),
                samer_expenses = closing.SamerExpenses.Select(e => new { amount = e.Amount, note = e.Note, receiver_id = e.ReceiverId, receiver = e.Receiver?.Name ?? "Unassigned" }),
                advances = closing.Advances.Select(a => new { amount = a.Amount, note = a.Note, employee_id = a.EmployeeId, employee = a.Employee?.Name ?? "Unassigned" }),
                deductions = closing.Deductions.Select(d => new { amount = d.Amount, note = d.Note, employee_id = d.EmployeeId, employee = d.Employee?.Name ?? "Unassigned" }),
                credits = closing.Credits.Select(cr => new { amount = cr.Amount, note = cr.Note, customer_id = cr.CustomerId, customer = cr.Customer?.Username ?? "Unassigned" }),
                cashbacks = closing.Cashbacks.Select(cb => new { amount = cb.Amount, note = cb.Note, customer_id = cb.CustomerId, customer = cb.Customer?.Username ?? "Unassigned" })
            });
        }

        // GET: /control-panel/daily-close/5/print
        [HttpGet("/control-panel/daily-close/{id:int}/print")]
        public async Task<IActionResult> PrintDailyClose(int id)
        {
            var closing = await _context.DailyClosings
                .Include(c => c.Expenses).ThenInclude(e => e.Receiver)
                .Include(c => c.AhmadMistrahExpenses).ThenInclude(e => e.Receiver)
                .Include(c => c.SamerExpenses).ThenInclude(e => e.Receiver)
                .Include(c => c.Advances).ThenInclude(a => a.Employee)
                .Include(c => c.Deductions).ThenInclude(d => d.Employee)
                .Include(c => c.Credits).ThenInclude(c => c.Customer)
                .Include(c => c.Cashbacks).ThenInclude(c => c.Customer)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (closing == null) return NotFound("Daily Closing record not found.");

            return View("DailyClosePrint", closing);
        }

        // GET: /DailyClosing
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var list = await _dailyClosingService.GetDailyClosingsAsync();
            if (Request.Path.Value?.Contains("/api/") == true)
            {
                return Json(list);
            }
            return View(list);
        }

        // GET: /DailyClosing/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetDailyClosing(int id)
        {
            _logger.LogInformation("Retrieving DailyClosing with ID: {ResourceId}", id);
            var dc = await _dailyClosingService.GetDailyClosingAsync(id);
            if (dc == null)
            {
                _logger.LogWarning("DailyClosing with ID {ResourceId} was not found.", id);
                return NotFound();
            }
            return Ok(dc);
        }

        // GET: /DailyClosing/preview?date=2026-06-12
        [HttpGet("preview")]
        public async Task<IActionResult> PreviewDailyClosing([FromQuery] DateTime date)
        {
            _logger.LogInformation("Generating DailyClosing preview for date: {Date}", date);
            var preview = await _dailyClosingService.GetDailyClosingPreviewAsync(date);
            return Ok(preview);
        }

        #region API Endpoints for script.js

        // GET: /api/daily-closing/2026-06-12
        [HttpGet("/api/daily-closing/{dateStr}")]
        public async Task<IActionResult> GetDailyClosingByDate(string dateStr)
        {
            if (!DateTime.TryParseExact(dateStr, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var targetDate))
            {
                return BadRequest(new { error = "Invalid date format. Expected yyyy-MM-dd" });
            }

            var closing = await _context.DailyClosings
                .FirstOrDefaultAsync(d => d.Date.Date == targetDate.Date);

            if (closing == null)
            {
                return NotFound(new { error = "No record found for this date" });
            }

            var expenses = await _context.Expenses.Where(e => e.DailyClosingId == closing.Id).ToListAsync();
            var advances = await _context.Advances.Where(a => a.DailyClosingId == closing.Id).ToListAsync();
            var credits = await _context.Credits.Where(c => c.DailyClosingId == closing.Id).ToListAsync();
            var cashbacks = await _context.Cashbacks.Where(cb => cb.DailyClosingId == closing.Id).ToListAsync();
            var deductions = await _context.Deductions.Where(d => d.DailyClosingId == closing.Id).ToListAsync();
            var samerExpenses = await _context.SamerExpenses.Where(se => se.DailyClosingId == closing.Id).ToListAsync();

            return Json(new {
                status = "success",
                data = new {
                    id = closing.Id,
                    date = closing.Date.ToString("yyyy-MM-dd"),
                    main_reading = closing.MainReading,
                    dr_smashed = closing.DrSmashed,
                    adjusted_reading = closing.AdjustedReading,
                    total_expenses = closing.TotalExpenses,
                    total_advance = closing.TotalAdvance,
                    total_credit = closing.TotalCredit,
                    total_cashback = closing.TotalCashback,
                    total_deductions = closing.TotalDeductions,
                    five_percent = closing.FivePercent,
                    total_cashout = closing.TotalCashout,
                    actual_cash = closing.ActualCash,
                    note = "",
                    expenses = expenses.Select(e => new { receiver_id = e.ReceiverId, amount = e.Amount, note = e.Note }),
                    advances = advances.Select(a => new { employee_id = a.EmployeeId, amount = a.Amount, note = a.Note }),
                    credits = credits.Select(c => new { customer_id = c.CustomerId, amount = c.Amount, note = c.Note }),
                    cashbacks = cashbacks.Select(cb => new { customer_id = cb.CustomerId, amount = cb.Amount, note = cb.Note }),
                    deductions = deductions.Select(d => new { employee_id = d.EmployeeId, amount = d.Amount, note = d.Note }),
                    samer_expenses = samerExpenses.Select(s => new { receiver_id = s.ReceiverId, amount = s.Amount, note = s.Note })
                }
            });
        }

        // POST: /api/daily-closing
        [HttpPost("/api/daily-closing")]
        public async Task<IActionResult> SaveDailyClosing([FromBody] DailyClosingSaveRequest data)
        {
            try
            {
                if (data == null || string.IsNullOrEmpty(data.Date))
                {
                    _logger.LogWarning("SaveDailyClosing failed: Null data or missing Date.");
                    return BadRequest(new { error = "Date is required" });
                }

                _logger.LogInformation("Saving DailyClosing for date: {Date}", data.Date);
                if (!DateTime.TryParseExact(data.Date, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var closeDate))
                {
                    _logger.LogWarning("SaveDailyClosing failed: Invalid date format {DateFormat}", data.Date);
                    return BadRequest(new { error = "Invalid date format. Expected yyyy-MM-dd" });
                }

                if (data.MainReading <= 0)
                {
                    _logger.LogWarning("SaveDailyClosing failed: MainReading is zero or negative.");
                    return BadRequest(new { error = "Main Reading is mandatory. Please provide the current counter value." });
                }

                var existing = await _context.DailyClosings.FirstOrDefaultAsync(d => d.Date.Date == closeDate.Date);
                if (existing != null)
                {
                    _logger.LogWarning("SaveDailyClosing failed: A record already exists for date: {Date}", data.Date);
                    return BadRequest(new { error = $"A daily closing record already exists for {data.Date}. Please edit the existing record or choose another date." });
                }

                decimal totalGen = data.Expenses.Sum(e => e.Amount);
                decimal totalSamer = data.SamerExpenses.Sum(e => e.Amount);
                decimal aggregatedExpenses = totalGen + totalSamer;

                var dailyClose = new DailyClosing
                {
                    Date = closeDate,
                    MainReading = data.MainReading,
                    DrSmashed = data.DrSmashed,
                    AdjustedReading = data.AdjustedReading,
                    TotalExpenses = aggregatedExpenses,
                    TotalAdvance = data.TotalAdvance,
                    TotalCredit = data.TotalCredit,
                    TotalCashback = data.TotalCashback,
                    TotalDeductions = data.TotalDeductions,
                    FivePercent = data.FivePercent,
                    TotalCashout = aggregatedExpenses + data.TotalAdvance + data.TotalDeductions,
                    ActualCash = data.ActualCash
                };

                _context.DailyClosings.Add(dailyClose);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully created DailyClosing record with ID: {ResourceId} for date: {Date}", dailyClose.Id, data.Date);

                // Audit log
                try
                {
                    var newValues = new { dailyClose.Date, dailyClose.MainReading, dailyClose.DrSmashed, dailyClose.AdjustedReading, dailyClose.ActualCash };
                    var username = User?.Identity?.Name ?? "Unknown";
                    var userId = (await _context.Users.FirstOrDefaultAsync(u => u.UserName == username))?.Id;
                    var ipAddress = HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
                    await _auditLogService.LogAddAsync("DailyClosing", dailyClose.Id, JsonSerializer.Serialize(newValues), username, userId, ipAddress, $"Daily closing created for date {data.Date}");
                    _logger.LogInformation("Audit log created for DailyClosing ID: {ResourceId}", dailyClose.Id);
                }
                catch (Exception auditEx)
                {
                    _logger.LogError(auditEx, "Failed to create audit log for DailyClosing ID: {ResourceId}", dailyClose.Id);
                }

            // Process Expenses
            foreach (var expData in data.Expenses)
            {
                var receiver = await _context.Receivers.FindAsync(expData.ReceiverId);
                if (receiver != null)
                {
                    receiver.PaidAmount += expData.Amount;
                    var expense = new Expenses
                    {
                        Date = closeDate,
                        Amount = expData.Amount,
                        Note = expData.Note,
                        DailyClosingId = dailyClose.Id,
                        ReceiverId = receiver.Id
                    };
                    _context.Expenses.Add(expense);
                }
            }

            // Process Samer Expenses
            foreach (var seData in data.SamerExpenses)
            {
                var receiver = await _context.SamerExpenseReceivers.FindAsync(seData.ReceiverId);
                if (receiver != null)
                {
                    receiver.PaidAmount += seData.Amount;
                    var expense = new SamerExpenses
                    {
                        Date = closeDate,
                        Amount = seData.Amount,
                        Note = seData.Note,
                        DailyClosingId = dailyClose.Id,
                        ReceiverId = receiver.Id
                    };
                    _context.SamerExpenses.Add(expense);
                }
            }

            // Process Advances
            foreach (var advData in data.Advances)
            {
                var employee = await _context.Employees.FindAsync(advData.EmployeeId);
                if (employee != null)
                {
                    var workingRecord = await _context.EmployeeWorkings
                        .FirstOrDefaultAsync(ew => ew.EmployeeId == employee.Id && ew.Month == closeDate.Month && ew.Year == closeDate.Year);

                    if (workingRecord == null)
                    {
                        decimal carryoverDebt = await ApplyCarryoverDebtAsync(employee.Id, closeDate.Year, closeDate.Month);
                        workingRecord = new EmployeeWorking
                        {
                            EmployeeId = employee.Id,
                            Month = closeDate.Month,
                            Year = closeDate.Year,
                            DeductionsTotal = carryoverDebt,
                            ActualSalary = -carryoverDebt,
                            WorkingDays = 30,
                            ActualWorkingDays = 0,
                            IsWorking = true
                        };
                        _context.EmployeeWorkings.Add(workingRecord);
                        await _context.SaveChangesAsync();
                    }

                    var advance = new Advances
                    {
                        Date = closeDate,
                        Amount = advData.Amount,
                        Note = advData.Note,
                        DailyClosingId = dailyClose.Id,
                        EmployeeId = employee.Id
                    };
                    _context.Advances.Add(advance);

                    workingRecord.AdvanceTotal += advData.Amount;
                    workingRecord.CalculateSalary();
                }
            }

            // Process Deductions
            foreach (var dedData in data.Deductions)
            {
                var employee = await _context.Employees.FindAsync(dedData.EmployeeId);
                if (employee != null)
                {
                    var workingRecord = await _context.EmployeeWorkings
                        .FirstOrDefaultAsync(ew => ew.EmployeeId == employee.Id && ew.Month == closeDate.Month && ew.Year == closeDate.Year);

                    if (workingRecord == null)
                    {
                        decimal carryoverDebt = await ApplyCarryoverDebtAsync(employee.Id, closeDate.Year, closeDate.Month);
                        workingRecord = new EmployeeWorking
                        {
                            EmployeeId = employee.Id,
                            Month = closeDate.Month,
                            Year = closeDate.Year,
                            DeductionsTotal = carryoverDebt,
                            ActualSalary = -carryoverDebt,
                            WorkingDays = 30,
                            ActualWorkingDays = 0,
                            IsWorking = true
                        };
                        _context.EmployeeWorkings.Add(workingRecord);
                        await _context.SaveChangesAsync();
                    }

                    var deduction = new Deductions
                    {
                        Date = closeDate,
                        Amount = dedData.Amount,
                        Note = dedData.Note,
                        DailyClosingId = dailyClose.Id,
                        EmployeeId = employee.Id
                    };
                    _context.Deductions.Add(deduction);

                    workingRecord.DeductionsTotal += dedData.Amount;
                    workingRecord.CalculateSalary();
                }
            }

            // Process Credits
            foreach (var crData in data.Credits)
            {
                var customer = await _context.Customers.FindAsync(crData.CustomerId);
                if (customer != null)
                {
                    customer.Balance -= crData.Amount;
                    var credit = new Credits
                    {
                        Date = closeDate,
                        Amount = crData.Amount,
                        Note = crData.Note,
                        DailyClosingId = dailyClose.Id,
                        CustomerId = customer.Id
                    };
                    _context.Credits.Add(credit);
                }
            }

            // Process Cashbacks
            foreach (var cbData in data.Cashbacks)
            {
                var customer = await _context.Customers.FindAsync(cbData.CustomerId);
                if (customer != null)
                {
                    customer.Balance += cbData.Amount;
                    var cashback = new Cashbacks
                    {
                        Date = closeDate,
                        Amount = cbData.Amount,
                        Note = cbData.Note,
                        DailyClosingId = dailyClose.Id,
                        CustomerId = customer.Id
                    };
                    _context.Cashbacks.Add(cashback);
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully created DailyClosing record with ID: {ResourceId} for date: {Date}", dailyClose.Id, data.Date);
            return Json(new { status = "success", id = dailyClose.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save DailyClosing for date: {Date}", data.Date);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // PUT: /api/daily-closing/2026-06-12
        [HttpPut("/api/daily-closing/{dateStr}")]
        public async Task<IActionResult> UpdateDailyClosingByDate(string dateStr, [FromBody] DailyClosingSaveRequest data)
        {
            try
            {
                if (data == null)
                {
                    _logger.LogWarning("UpdateDailyClosingByDate failed: Null payload for date {Date}", dateStr);
                    return BadRequest(new { error = "Invalid payload" });
                }

                _logger.LogInformation("Updating DailyClosing for date: {Date}", dateStr);
                if (!DateTime.TryParseExact(dateStr, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var closeDate))
                {
                    _logger.LogWarning("UpdateDailyClosingByDate failed: Invalid date format {DateFormat}", dateStr);
                    return BadRequest(new { error = "Invalid date format. Expected yyyy-MM-dd" });
                }

                var closing = await _context.DailyClosings.FirstOrDefaultAsync(d => d.Date.Date == closeDate.Date);
                if (closing == null)
                {
                    _logger.LogWarning("UpdateDailyClosingByDate failed: No record found for date: {Date}", dateStr);
                    return NotFound(new { error = "No record found for this date" });
                }

                // Capture old values for audit log
                var oldValues = new { closing.Date, closing.MainReading, closing.DrSmashed, closing.AdjustedReading, closing.ActualCash };

                await DeleteDailyClosingItemsAsync(closing);
                await _context.SaveChangesAsync();

            decimal totalGen = data.Expenses.Sum(e => e.Amount);
            decimal totalSamer = data.SamerExpenses.Sum(e => e.Amount);
            decimal aggregatedExpenses = totalGen + totalSamer;

            closing.MainReading = data.MainReading;
            closing.DrSmashed = data.DrSmashed;
            closing.AdjustedReading = data.AdjustedReading;
            closing.TotalExpenses = aggregatedExpenses;
            closing.TotalAdvance = data.TotalAdvance;
            closing.TotalCredit = data.TotalCredit;
            closing.TotalCashback = data.TotalCashback;
            closing.TotalDeductions = data.TotalDeductions;
            closing.FivePercent = data.FivePercent;
            closing.TotalCashout = aggregatedExpenses + data.TotalAdvance + data.TotalDeductions;
            closing.ActualCash = data.ActualCash;

            // Process Expenses
            foreach (var expData in data.Expenses)
            {
                var receiver = await _context.Receivers.FindAsync(expData.ReceiverId);
                if (receiver != null)
                {
                    receiver.PaidAmount += expData.Amount;
                    var expense = new Expenses
                    {
                        Date = closeDate,
                        Amount = expData.Amount,
                        Note = expData.Note,
                        DailyClosingId = closing.Id,
                        ReceiverId = receiver.Id
                    };
                    _context.Expenses.Add(expense);
                }
            }

            // Process Samer Expenses
            foreach (var seData in data.SamerExpenses)
            {
                var receiver = await _context.SamerExpenseReceivers.FindAsync(seData.ReceiverId);
                if (receiver != null)
                {
                    receiver.PaidAmount += seData.Amount;
                    var expense = new SamerExpenses
                    {
                        Date = closeDate,
                        Amount = seData.Amount,
                        Note = seData.Note,
                        DailyClosingId = closing.Id,
                        ReceiverId = receiver.Id
                    };
                    _context.SamerExpenses.Add(expense);
                }
            }

            // Process Advances
            foreach (var advData in data.Advances)
            {
                var employee = await _context.Employees.FindAsync(advData.EmployeeId);
                if (employee != null)
                {
                    var workingRecord = await _context.EmployeeWorkings
                        .FirstOrDefaultAsync(ew => ew.EmployeeId == employee.Id && ew.Month == closeDate.Month && ew.Year == closeDate.Year);

                    if (workingRecord == null)
                    {
                        decimal carryoverDebt = await ApplyCarryoverDebtAsync(employee.Id, closeDate.Year, closeDate.Month);
                        workingRecord = new EmployeeWorking
                        {
                            EmployeeId = employee.Id,
                            Month = closeDate.Month,
                            Year = closeDate.Year,
                            DeductionsTotal = carryoverDebt,
                            ActualSalary = -carryoverDebt,
                            WorkingDays = 30,
                            ActualWorkingDays = 0,
                            IsWorking = true
                        };
                        _context.EmployeeWorkings.Add(workingRecord);
                        await _context.SaveChangesAsync();
                    }

                    var advance = new Advances
                    {
                        Date = closeDate,
                        Amount = advData.Amount,
                        Note = advData.Note,
                        DailyClosingId = closing.Id,
                        EmployeeId = employee.Id
                    };
                    _context.Advances.Add(advance);

                    workingRecord.AdvanceTotal += advData.Amount;
                    workingRecord.CalculateSalary();
                }
            }

            // Process Deductions
            foreach (var dedData in data.Deductions)
            {
                var employee = await _context.Employees.FindAsync(dedData.EmployeeId);
                if (employee != null)
                {
                    var workingRecord = await _context.EmployeeWorkings
                        .FirstOrDefaultAsync(ew => ew.EmployeeId == employee.Id && ew.Month == closeDate.Month && ew.Year == closeDate.Year);

                    if (workingRecord == null)
                    {
                        decimal carryoverDebt = await ApplyCarryoverDebtAsync(employee.Id, closeDate.Year, closeDate.Month);
                        workingRecord = new EmployeeWorking
                        {
                            EmployeeId = employee.Id,
                            Month = closeDate.Month,
                            Year = closeDate.Year,
                            DeductionsTotal = carryoverDebt,
                            ActualSalary = -carryoverDebt,
                            WorkingDays = 30,
                            ActualWorkingDays = 0,
                            IsWorking = true
                        };
                        _context.EmployeeWorkings.Add(workingRecord);
                        await _context.SaveChangesAsync();
                    }

                    var deduction = new Deductions
                    {
                        Date = closeDate,
                        Amount = dedData.Amount,
                        Note = dedData.Note,
                        DailyClosingId = closing.Id,
                        EmployeeId = employee.Id
                    };
                    _context.Deductions.Add(deduction);

                    workingRecord.DeductionsTotal += dedData.Amount;
                    workingRecord.CalculateSalary();
                }
            }

            // Process Credits
            foreach (var crData in data.Credits)
            {
                var customer = await _context.Customers.FindAsync(crData.CustomerId);
                if (customer != null)
                {
                    customer.Balance -= crData.Amount;
                    var credit = new Credits
                    {
                        Date = closeDate,
                        Amount = crData.Amount,
                        Note = crData.Note,
                        DailyClosingId = closing.Id,
                        CustomerId = customer.Id
                    };
                    _context.Credits.Add(credit);
                }
            }

            // Process Cashbacks
            foreach (var cbData in data.Cashbacks)
            {
                var customer = await _context.Customers.FindAsync(cbData.CustomerId);
                if (customer != null)
                {
                    customer.Balance += cbData.Amount;
                    var cashback = new Cashbacks
                    {
                        Date = closeDate,
                        Amount = cbData.Amount,
                        Note = cbData.Note,
                        DailyClosingId = closing.Id,
                        CustomerId = customer.Id
                    };
                    _context.Cashbacks.Add(cashback);
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully updated DailyClosing record ID: {ResourceId} for date: {Date}", closing.Id, dateStr);

            // Audit log
            try
            {
                var newValues = new { closing.Date, closing.MainReading, closing.DrSmashed, closing.AdjustedReading, closing.ActualCash };
                var username = User?.Identity?.Name ?? "Unknown";
                var userId = (await _context.Users.FirstOrDefaultAsync(u => u.UserName == username))?.Id;
                var ipAddress = HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
                await _auditLogService.LogUpdateAsync("DailyClosing", closing.Id, JsonSerializer.Serialize(oldValues), JsonSerializer.Serialize(newValues), username, userId, ipAddress, $"Daily closing updated for date {dateStr}");
                _logger.LogInformation("Audit log created for DailyClosing ID: {ResourceId}", closing.Id);
            }
            catch (Exception auditEx)
            {
                _logger.LogError(auditEx, "Failed to create audit log for DailyClosing ID: {ResourceId}", closing.Id);
            }

            return Json(new { status = "success", id = closing.Id, message = "Daily closing updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update DailyClosing for date: {Date}", dateStr);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // DELETE: /api/daily-closing/{dateStr}
        [HttpDelete("/api/daily-closing/{dateStr}")]
        public async Task<IActionResult> DeleteDailyClosingByDate(string dateStr)
        {
            try
            {
                _logger.LogWarning("Initiating deletion of DailyClosing for date: {Date}", dateStr);
                if (!DateTime.TryParseExact(dateStr, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var targetDate))
                {
                    _logger.LogWarning("DeleteDailyClosingByDate failed: Invalid date format {DateFormat}", dateStr);
                    return BadRequest(new { error = "Invalid date format. Expected yyyy-MM-dd" });
                }

                var closing = await _context.DailyClosings
                    .FirstOrDefaultAsync(d => d.Date.Date == targetDate.Date);

                if (closing == null)
                {
                    _logger.LogWarning("DeleteDailyClosingByDate failed: Record not found for date: {Date}", dateStr);
                    return NotFound(new { error = "No record found for this date" });
                }

                // Capture old values for audit log
                var oldValues = new { closing.Date, closing.MainReading, closing.DrSmashed, closing.AdjustedReading, closing.ActualCash };

                await DeleteDailyClosingItemsAsync(closing);
                _context.DailyClosings.Remove(closing);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully deleted DailyClosing record ID: {ResourceId} for date: {Date}", closing.Id, dateStr);

                // Audit log
                try
                {
                    var username = User?.Identity?.Name ?? "Unknown";
                    var userId = (await _context.Users.FirstOrDefaultAsync(u => u.UserName == username))?.Id;
                    var ipAddress = HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
                    await _auditLogService.LogDeleteAsync("DailyClosing", closing.Id, JsonSerializer.Serialize(oldValues), username, userId, ipAddress, $"Daily closing deleted for date {dateStr}");
                    _logger.LogInformation("Audit log created for DailyClosing ID: {ResourceId}", closing.Id);
                }
                catch (Exception auditEx)
                {
                    _logger.LogError(auditEx, "Failed to create audit log for DailyClosing ID: {ResourceId}", closing.Id);
                }
                return Json(new { status = "success", message = "Daily closing deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete DailyClosing for date: {Date}", dateStr);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        #endregion

        #region Helper Methods

        private async Task DeleteDailyClosingItemsAsync(DailyClosing closing)
        {
            var expenses = await _context.Expenses.Where(e => e.DailyClosingId == closing.Id).ToListAsync();
            foreach (var exp in expenses)
            {
                if (exp.ReceiverId.HasValue)
                {
                    var receiver = await _context.Receivers.FindAsync(exp.ReceiverId);
                    if (receiver != null) receiver.PaidAmount -= exp.Amount;
                }
                _context.Expenses.Remove(exp);
            }

            var samerExpenses = await _context.SamerExpenses.Where(se => se.DailyClosingId == closing.Id).ToListAsync();
            foreach (var se in samerExpenses)
            {
                if (se.ReceiverId.HasValue)
                {
                    var receiver = await _context.SamerExpenseReceivers.FindAsync(se.ReceiverId);
                    if (receiver != null) receiver.PaidAmount -= se.Amount;
                }
                _context.SamerExpenses.Remove(se);
            }

            var advances = await _context.Advances.Where(a => a.DailyClosingId == closing.Id).ToListAsync();
            foreach (var adv in advances)
            {
                if (adv.EmployeeId.HasValue)
                {
                    var workingRecord = await _context.EmployeeWorkings
                        .FirstOrDefaultAsync(ew => ew.EmployeeId == adv.EmployeeId && ew.Month == adv.Date.Month && ew.Year == adv.Date.Year);
                    if (workingRecord != null)
                    {
                        workingRecord.AdvanceTotal -= adv.Amount;
                        workingRecord.CalculateSalary();
                    }
                }
                _context.Advances.Remove(adv);
            }

            var deductions = await _context.Deductions.Where(d => d.DailyClosingId == closing.Id).ToListAsync();
            foreach (var ded in deductions)
            {
                if (ded.EmployeeId.HasValue)
                {
                    var workingRecord = await _context.EmployeeWorkings
                        .FirstOrDefaultAsync(ew => ew.EmployeeId == ded.EmployeeId && ew.Month == ded.Date.Month && ew.Year == ded.Date.Year);
                    if (workingRecord != null)
                    {
                        workingRecord.DeductionsTotal -= ded.Amount;
                        workingRecord.CalculateSalary();
                    }
                }
                _context.Deductions.Remove(ded);
            }

            var credits = await _context.Credits.Where(c => c.DailyClosingId == closing.Id).ToListAsync();
            foreach (var cred in credits)
            {
                if (cred.CustomerId.HasValue)
                {
                    var customer = await _context.Customers.FindAsync(cred.CustomerId);
                    if (customer != null) customer.Balance += cred.Amount;
                }
                _context.Credits.Remove(cred);
            }

            var cashbacks = await _context.Cashbacks.Where(cb => cb.DailyClosingId == closing.Id).ToListAsync();
            foreach (var cb in cashbacks)
            {
                if (cb.CustomerId.HasValue)
                {
                    var customer = await _context.Customers.FindAsync(cb.CustomerId);
                    if (customer != null) customer.Balance -= cb.Amount;
                }
                _context.Cashbacks.Remove(cb);
            }
        }

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

    #region Custom Request DTOs

    public class DailyClosingSaveRequest
    {
        public string Date { get; set; } = string.Empty;
        [JsonPropertyName("main_reading")]
        public decimal MainReading { get; set; }
        [JsonPropertyName("dr_smashed")]
        public decimal DrSmashed { get; set; }
        [JsonPropertyName("adjusted_reading")]
        public decimal AdjustedReading { get; set; }
        [JsonPropertyName("total_expenses")]
        public decimal TotalExpenses { get; set; }
        [JsonPropertyName("total_advance")]
        public decimal TotalAdvance { get; set; }
        [JsonPropertyName("total_credit")]
        public decimal TotalCredit { get; set; }
        [JsonPropertyName("total_cashback")]
        public decimal TotalCashback { get; set; }
        [JsonPropertyName("total_deductions")]
        public decimal TotalDeductions { get; set; }
        [JsonPropertyName("five_percent")]
        public decimal FivePercent { get; set; }
        [JsonPropertyName("actual_cash")]
        public decimal ActualCash { get; set; }
        public string Note { get; set; } = string.Empty;

        public List<ExpenseSaveItem> Expenses { get; set; } = new();
        [JsonPropertyName("samer_expenses")]
        public List<ExpenseSaveItem> SamerExpenses { get; set; } = new();
        public List<AdvanceSaveItem> Advances { get; set; } = new();
        public List<DeductionSaveItem> Deductions { get; set; } = new();
        public List<CreditSaveItem> Credits { get; set; } = new();
        public List<CashbackSaveItem> Cashbacks { get; set; } = new();
    }

    public class ExpenseSaveItem
    {
        [JsonPropertyName("receiver_id")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int ReceiverId { get; set; }
        public decimal Amount { get; set; }
        public string Note { get; set; } = string.Empty;
    }

    public class AdvanceSaveItem
    {
        [JsonPropertyName("employee_id")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int EmployeeId { get; set; }
        public decimal Amount { get; set; }
        public string Note { get; set; } = string.Empty;
    }

    public class DeductionSaveItem
    {
        [JsonPropertyName("employee_id")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int EmployeeId { get; set; }
        public decimal Amount { get; set; }
        public string Note { get; set; } = string.Empty;
    }

    public class CreditSaveItem
    {
        [JsonPropertyName("customer_id")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int CustomerId { get; set; }
        public decimal Amount { get; set; }
        public string Note { get; set; } = string.Empty;
    }

    public class CashbackSaveItem
    {
        [JsonPropertyName("customer_id")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int CustomerId { get; set; }
        public decimal Amount { get; set; }
        public string Note { get; set; } = string.Empty;
    }

    #endregion
}
