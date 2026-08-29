using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiskBeirut.Application.DTOs;
using MiskBeirut.Application.Services;
using MiskBeirut.Core.Entities;
using MiskBeirut.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MiskBeirut.Web.Controllers
{
    [Route("[controller]")]
    [Route("api/[controller]")]
    public class ExpensesController : Controller
    {
        private readonly IExpensesService _expensesService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ExpensesController> _logger;

        public ExpensesController(IExpensesService expensesService, ApplicationDbContext context, ILogger<ExpensesController> logger)
        {
            _expensesService = expensesService;
            _context = context;
            _logger = logger;
        }

        // GET: /Expenses
        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] int? month, [FromQuery] int? year, [FromQuery] int? receiver_id)
        {
            var now = DateTime.UtcNow;
            int selectedMonth = month ?? now.Month;
            int selectedYear = year ?? now.Year;

            var query = _context.Expenses
                .Include(e => e.Receiver)
                .AsQueryable();

            if (month.HasValue)
            {
                query = query.Where(e => e.Date.Month == selectedMonth);
            }
            if (year.HasValue)
            {
                query = query.Where(e => e.Date.Year == selectedYear);
            }
            if (receiver_id.HasValue)
            {
                query = query.Where(e => e.ReceiverId == receiver_id.Value);
            }

            var expensesList = await query.OrderByDescending(e => e.Date).ToListAsync();
            var receiversList = await _context.Receivers.OrderBy(r => r.Name).ToListAsync();

            // Calculate statistics
            decimal totalExpenses = expensesList.Sum(e => e.Amount);
            decimal maxExpense = expensesList.Any() ? expensesList.Max(e => e.Amount) : 0m;
            int count = expensesList.Count;

            var receiverTotals = new Dictionary<string, decimal>();
            foreach (var exp in expensesList)
            {
                string name = exp.Receiver?.Name ?? "N/A";
                if (receiverTotals.ContainsKey(name))
                    receiverTotals[name] += exp.Amount;
                else
                    receiverTotals[name] = exp.Amount;
            }

            var topReceiver = receiverTotals.Any() 
                ? receiverTotals.OrderByDescending(x => x.Value).First() 
                : new KeyValuePair<string, decimal>("N/A", 0m);

            var stats = new ExpensesStats
            {
                Total = totalExpenses,
                Max = maxExpense,
                Count = count,
                TopReceiver = topReceiver.Key,
                TopReceiverAmount = topReceiver.Value
            };

            var viewModel = new ExpensesViewModel
            {
                Expenses = expensesList,
                Receivers = receiversList,
                Stats = stats,
                CurrentReceiverId = receiver_id,
                CurrentMonth = selectedMonth,
                CurrentYear = selectedYear
            };

            if (Request.Path.Value?.Contains("/api/") == true)
            {
                return Json(new { status = "success", expenses = expensesList });
            }

            return View(viewModel);
        }

        // GET: /Expenses/5 or /api/Expenses/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetExpense(int id)
        {
            var exp = await _context.Expenses
                .Include(e => e.Receiver)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (exp == null) return NotFound();

            return Json(new {
                id = exp.Id,
                date = exp.Date.ToString("yyyy-MM-dd"),
                amount = exp.Amount,
                note = exp.Note,
                receiver_id = exp.ReceiverId,
                receiver_name = exp.Receiver?.Name
            });
        }

        // POST: /Expenses or /api/Expenses
        [HttpPost]
        public async Task<IActionResult> CreateExpense([FromBody] ExpenseApiRequest request)
        {
            if (request == null) return BadRequest(new { error = "Invalid payload" });

            try
            {
                int? receiverId = request.ReceiverId;
                if (!string.IsNullOrWhiteSpace(request.ReceiverName))
                {
                    var receiver = await _context.Receivers.FirstOrDefaultAsync(r => r.Name == request.ReceiverName);
                    if (receiver == null)
                    {
                        receiver = new Receivers { Name = request.ReceiverName, PaidAmount = 0m };
                        _context.Receivers.Add(receiver);
                        await _context.SaveChangesAsync();
                    }
                    receiverId = receiver.Id;
                }

                DateTime date = DateTime.UtcNow;
                if (!string.IsNullOrEmpty(request.Date) && DateTime.TryParse(request.Date, out var parsedDate))
                {
                    date = parsedDate;
                }

                var expense = new Expenses
                {
                    Date = date,
                    Amount = request.Amount,
                    Note = request.Note,
                    ReceiverId = receiverId,
                    DailyClosingId = request.DailyClosingId
                };

                _context.Expenses.Add(expense);

                if (receiverId.HasValue)
                {
                    var rec = await _context.Receivers.FindAsync(receiverId.Value);
                    if (rec != null)
                    {
                        rec.PaidAmount += request.Amount;
                    }
                }

                await SyncDailyClosingTotalAsync(date, "total_expenses", request.Amount);
                await _context.SaveChangesAsync();

                // Add log event
                await LogEventAsync("SUCCESS", "expense_create", $"Expense of {request.Amount} created successfully", $"{{\"id\": {expense.Id}, \"amount\": \"{request.Amount}\"}}");

                return Json(new { status = "success", message = "Expense created.", id = expense.Id });
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", error = ex.Message });
            }
        }

        // PUT: /Expenses/5 or /api/Expenses/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateExpense(int id, [FromBody] ExpenseApiRequest request)
        {
            if (request == null) return BadRequest(new { error = "Invalid payload" });

            try
            {
                var expense = await _context.Expenses.FindAsync(id);
                if (expense == null) return NotFound();

                int? receiverId = request.ReceiverId;
                if (!string.IsNullOrWhiteSpace(request.ReceiverName))
                {
                    var receiver = await _context.Receivers.FirstOrDefaultAsync(r => r.Name == request.ReceiverName);
                    if (receiver == null)
                    {
                        receiver = new Receivers { Name = request.ReceiverName, PaidAmount = 0m };
                        _context.Receivers.Add(receiver);
                        await _context.SaveChangesAsync();
                    }
                    receiverId = receiver.Id;
                }

                DateTime newDate = expense.Date;
                if (!string.IsNullOrEmpty(request.Date) && DateTime.TryParse(request.Date, out var parsedDate))
                {
                    newDate = parsedDate;
                }

                decimal oldAmount = expense.Amount;
                DateTime oldDate = expense.Date;

                // Sync daily closing
                if (oldDate.Date == newDate.Date)
                {
                    decimal diff = request.Amount - oldAmount;
                    if (diff != 0)
                    {
                        await SyncDailyClosingTotalAsync(oldDate, "total_expenses", diff);
                    }
                }
                else
                {
                    await SyncDailyClosingTotalAsync(oldDate, "total_expenses", -oldAmount);
                    await SyncDailyClosingTotalAsync(newDate, "total_expenses", request.Amount);
                }

                // Update Receiver paid amounts
                if (expense.ReceiverId != receiverId || expense.Amount != request.Amount)
                {
                    if (expense.ReceiverId.HasValue)
                    {
                        var oldRec = await _context.Receivers.FindAsync(expense.ReceiverId.Value);
                        if (oldRec != null) oldRec.PaidAmount -= expense.Amount;
                    }
                    if (receiverId.HasValue)
                    {
                        var newRec = await _context.Receivers.FindAsync(receiverId.Value);
                        if (newRec != null) newRec.PaidAmount += request.Amount;
                    }
                }

                expense.Date = newDate;
                expense.Amount = request.Amount;
                expense.Note = request.Note;
                expense.ReceiverId = receiverId;
                expense.DailyClosingId = request.DailyClosingId;

                await _context.SaveChangesAsync();

                // Add log event
                await LogEventAsync("SUCCESS", "expense_update", $"Expense of {request.Amount} updated successfully", $"{{\"id\": {expense.Id}, \"amount\": \"{request.Amount}\"}}");

                return Json(new { status = "success", message = "Expense updated." });
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", error = ex.Message });
            }
        }

        // DELETE: /Expenses/5 or /api/Expenses/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteExpense(int id)
        {
            try
            {
                var expense = await _context.Expenses.FindAsync(id);
                if (expense == null) return NotFound();

                decimal amount = expense.Amount;
                DateTime date = expense.Date;

                await SyncDailyClosingTotalAsync(date, "total_expenses", -amount);

                if (expense.ReceiverId.HasValue)
                {
                    var receiver = await _context.Receivers.FindAsync(expense.ReceiverId.Value);
                    if (receiver != null)
                    {
                        receiver.PaidAmount -= amount;
                    }
                }

                _context.Expenses.Remove(expense);
                await _context.SaveChangesAsync();

                // Add log event
                await LogEventAsync("SUCCESS", "expense_delete", $"Expense of {amount} deleted successfully", $"{{\"id\": {id}, \"amount\": \"{amount}\"}}");

                return Json(new { status = "success", ok = true, message = "Expense deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", error = ex.Message });
            }
        }

        // GET: /Expenses/ahmad
        [HttpGet("ahmad")]
        [HttpGet("/api/ahmad-expenses")]
        public async Task<IActionResult> GetAhmadExpenses([FromQuery] int? month, [FromQuery] int? year, [FromQuery] int? receiver_id)
        {
            var now = DateTime.UtcNow;
            int selectedMonth = month ?? now.Month;
            int selectedYear = year ?? now.Year;

            var query = _context.AhmadMistrahExpenses
                .Include(e => e.Receiver)
                .AsQueryable();

            if (month.HasValue)
            {
                query = query.Where(e => e.Date.Month == selectedMonth);
            }
            if (year.HasValue)
            {
                query = query.Where(e => e.Date.Year == selectedYear);
            }
            if (receiver_id.HasValue)
            {
                query = query.Where(e => e.ReceiverId == receiver_id.Value);
            }

            var expensesList = await query.OrderByDescending(e => e.Date).ToListAsync();
            var receiversList = await _context.AhmadExpenseReceivers.OrderBy(r => r.Name).ToListAsync();

            decimal totalExpenses = expensesList.Sum(e => e.Amount);
            decimal maxExpense = expensesList.Any() ? expensesList.Max(e => e.Amount) : 0m;
            int count = expensesList.Count;

            var receiverTotals = new Dictionary<string, decimal>();
            foreach (var exp in expensesList)
            {
                string name = exp.Receiver?.Name ?? "General";
                if (receiverTotals.ContainsKey(name))
                    receiverTotals[name] += exp.Amount;
                else
                    receiverTotals[name] = exp.Amount;
            }

            var topReceiver = receiverTotals.Any() 
                ? receiverTotals.OrderByDescending(x => x.Value).First() 
                : new KeyValuePair<string, decimal>("N/A", 0m);

            var stats = new ExpensesStats
            {
                Total = totalExpenses,
                Max = maxExpense,
                Count = count,
                TopReceiver = topReceiver.Key,
                TopReceiverAmount = topReceiver.Value
            };

            var viewModel = new AhmadExpensesViewModel
            {
                Expenses = expensesList,
                Receivers = receiversList,
                Stats = stats,
                ReceiverId = receiver_id,
                CurrentMonth = selectedMonth,
                CurrentYear = selectedYear
            };

            if (Request.Path.Value?.Contains("/api/") == true)
            {
                return Json(new {
                    status = "success",
                    expenses = expensesList.Select(e => new {
                        id = e.Id,
                        date = e.Date.ToString("yyyy-MM-dd"),
                        amount = e.Amount,
                        note = e.Note,
                        receiver_name = e.Receiver?.Name ?? "General"
                    }),
                    total = totalExpenses
                });
            }

            return View("Ahmad", viewModel);
        }

        // GET: /Expenses/ahmad/5
        [HttpGet("ahmad/{id:int}")]
        [HttpGet("/api/ahmad-expenses/{id:int}")]
        public async Task<IActionResult> GetAhmadExpense(int id)
        {
            var exp = await _context.AhmadMistrahExpenses
                .Include(e => e.Receiver)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (exp == null) return NotFound();

            return Json(new {
                id = exp.Id,
                date = exp.Date.ToString("yyyy-MM-dd"),
                amount = exp.Amount,
                note = exp.Note,
                receiver_id = exp.ReceiverId,
                receiver_name = exp.Receiver?.Name ?? "General"
            });
        }

        // POST: /Expenses/ahmad
        [HttpPost("ahmad")]
        [HttpPost("/api/ahmad-expenses")]
        public async Task<IActionResult> CreateAhmadExpense([FromBody] ExpenseApiRequest request)
        {
            if (request == null) return BadRequest(new { error = "Invalid payload" });

            try
            {
                // Strict validation: Receiver must exist
                if (!request.ReceiverId.HasValue)
                {
                    return BadRequest(new { error = "Valid receiver is required" });
                }

                var receiver = await _context.AhmadExpenseReceivers.FindAsync(request.ReceiverId.Value);
                if (receiver == null)
                {
                    return BadRequest(new { error = "Valid receiver is required" });
                }

                DateTime date = DateTime.UtcNow;
                if (!string.IsNullOrEmpty(request.Date) && DateTime.TryParse(request.Date, out var parsedDate))
                {
                    date = parsedDate;
                }

                var expense = new AhmadMistrahExpenses
                {
                    Date = date,
                    Amount = request.Amount,
                    Note = request.Note,
                    ReceiverId = receiver.Id,
                    DailyClosingId = request.DailyClosingId
                };

                _context.AhmadMistrahExpenses.Add(expense);
                receiver.PaidAmount += request.Amount;

                await SyncDailyClosingTotalAsync(date, "total_expenses", request.Amount);
                await _context.SaveChangesAsync();

                // Add log event
                await LogEventAsync("SUCCESS", "ahmad_expense_create", $"Ahmad expense of {request.Amount} created successfully", $"{{\"id\": {expense.Id}, \"amount\": \"{request.Amount}\"}}");

                return Json(new { status = "success", message = "Ahmad expense created.", id = expense.Id });
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", error = ex.Message });
            }
        }

        // PUT: /Expenses/ahmad/5
        [HttpPut("ahmad/{id:int}")]
        [HttpPut("/api/ahmad-expenses/{id:int}")]
        public async Task<IActionResult> UpdateAhmadExpense(int id, [FromBody] ExpenseApiRequest request)
        {
            if (request == null) return BadRequest(new { error = "Invalid payload" });

            try
            {
                var expense = await _context.AhmadMistrahExpenses.FindAsync(id);
                if (expense == null) return NotFound();

                if (!request.ReceiverId.HasValue)
                {
                    return BadRequest(new { error = "Valid receiver is required" });
                }

                var receiver = await _context.AhmadExpenseReceivers.FindAsync(request.ReceiverId.Value);
                if (receiver == null)
                {
                    return BadRequest(new { error = "Valid receiver is required" });
                }

                DateTime newDate = expense.Date;
                if (!string.IsNullOrEmpty(request.Date) && DateTime.TryParse(request.Date, out var parsedDate))
                {
                    newDate = parsedDate;
                }

                decimal oldAmount = expense.Amount;
                DateTime oldDate = expense.Date;

                // Sync daily closing
                if (oldDate.Date == newDate.Date)
                {
                    decimal diff = request.Amount - oldAmount;
                    if (diff != 0)
                    {
                        await SyncDailyClosingTotalAsync(oldDate, "total_expenses", diff);
                    }
                }
                else
                {
                    await SyncDailyClosingTotalAsync(oldDate, "total_expenses", -oldAmount);
                    await SyncDailyClosingTotalAsync(newDate, "total_expenses", request.Amount);
                }

                // Update Receiver paid amounts
                if (expense.ReceiverId != receiver.Id || expense.Amount != request.Amount)
                {
                    if (expense.ReceiverId.HasValue)
                    {
                        var oldRec = await _context.AhmadExpenseReceivers.FindAsync(expense.ReceiverId.Value);
                        if (oldRec != null) oldRec.PaidAmount -= expense.Amount;
                    }
                    receiver.PaidAmount += request.Amount;
                }

                expense.Date = newDate;
                expense.Amount = request.Amount;
                expense.Note = request.Note;
                expense.ReceiverId = receiver.Id;
                expense.DailyClosingId = request.DailyClosingId;

                await _context.SaveChangesAsync();

                // Add log event
                await LogEventAsync("SUCCESS", "ahmad_expense_update", $"Ahmad expense of {request.Amount} updated successfully", $"{{\"id\": {expense.Id}, \"amount\": \"{request.Amount}\"}}");

                return Json(new { status = "success", message = "Ahmad expense updated." });
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", error = ex.Message });
            }
        }

        // DELETE: /Expenses/ahmad/5
        [HttpDelete("ahmad/{id:int}")]
        [HttpDelete("/api/ahmad-expenses/{id:int}")]
        public async Task<IActionResult> DeleteAhmadExpense(int id)
        {
            try
            {
                var expense = await _context.AhmadMistrahExpenses.FindAsync(id);
                if (expense == null) return NotFound();

                decimal amount = expense.Amount;
                DateTime date = expense.Date;

                await SyncDailyClosingTotalAsync(date, "total_expenses", -amount);

                if (expense.ReceiverId.HasValue)
                {
                    var receiver = await _context.AhmadExpenseReceivers.FindAsync(expense.ReceiverId.Value);
                    if (receiver != null)
                    {
                        receiver.PaidAmount -= amount;
                    }
                }

                _context.AhmadMistrahExpenses.Remove(expense);
                await _context.SaveChangesAsync();

                // Add log event
                await LogEventAsync("SUCCESS", "ahmad_expense_delete", $"Ahmad expense of {amount} deleted successfully", $"{{\"id\": {id}, \"amount\": \"{amount}\"}}");

                return Json(new { status = "success", ok = true, message = "Ahmad expense deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", error = ex.Message });
            }
        }

        // GET: /Expenses/samer
        [HttpGet("samer")]
        [HttpGet("/api/samer-expenses")]
        public async Task<IActionResult> GetSamerExpenses([FromQuery] int? month, [FromQuery] int? year, [FromQuery] int? receiver_id)
        {
            var now = DateTime.UtcNow;
            int selectedMonth = month ?? now.Month;
            int selectedYear = year ?? now.Year;

            var query = _context.SamerExpenses
                .Include(e => e.Receiver)
                .AsQueryable();

            if (month.HasValue)
            {
                query = query.Where(e => e.Date.Month == selectedMonth);
            }
            if (year.HasValue)
            {
                query = query.Where(e => e.Date.Year == selectedYear);
            }
            if (receiver_id.HasValue)
            {
                query = query.Where(e => e.ReceiverId == receiver_id.Value);
            }

            var expensesList = await query.OrderByDescending(e => e.Date).ToListAsync();
            var receiversList = await _context.SamerExpenseReceivers.OrderBy(r => r.Name).ToListAsync();

            decimal totalExpenses = expensesList.Sum(e => e.Amount);
            decimal maxExpense = expensesList.Any() ? expensesList.Max(e => e.Amount) : 0m;
            int count = expensesList.Count;

            var receiverTotals = new Dictionary<string, decimal>();
            foreach (var exp in expensesList)
            {
                string name = exp.Receiver?.Name ?? "General";
                if (receiverTotals.ContainsKey(name))
                    receiverTotals[name] += exp.Amount;
                else
                    receiverTotals[name] = exp.Amount;
            }

            var topReceiver = receiverTotals.Any() 
                ? receiverTotals.OrderByDescending(x => x.Value).First() 
                : new KeyValuePair<string, decimal>("N/A", 0m);

            var stats = new ExpensesStats
            {
                Total = totalExpenses,
                Max = maxExpense,
                Count = count,
                TopReceiver = topReceiver.Key,
                TopReceiverAmount = topReceiver.Value
            };

            var viewModel = new SamerExpensesViewModel
            {
                Expenses = expensesList,
                Receivers = receiversList,
                Stats = stats,
                ReceiverId = receiver_id,
                CurrentMonth = selectedMonth,
                CurrentYear = selectedYear
            };

            if (Request.Path.Value?.Contains("/api/") == true)
            {
                return Json(new {
                    status = "success",
                    expenses = expensesList.Select(e => new {
                        id = e.Id,
                        date = e.Date.ToString("yyyy-MM-dd"),
                        amount = e.Amount,
                        note = e.Note,
                        receiver_name = e.Receiver?.Name ?? "General"
                    }),
                    total = totalExpenses
                });
            }

            return View("Samer", viewModel);
        }

        // GET: /Expenses/samer/5
        [HttpGet("samer/{id:int}")]
        [HttpGet("/api/samer-expenses/{id:int}")]
        public async Task<IActionResult> GetSamerExpense(int id)
        {
            var exp = await _context.SamerExpenses
                .Include(e => e.Receiver)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (exp == null) return NotFound();

            return Json(new {
                id = exp.Id,
                date = exp.Date.ToString("yyyy-MM-dd"),
                amount = exp.Amount,
                note = exp.Note,
                receiver_id = exp.ReceiverId,
                receiver_name = exp.Receiver?.Name ?? "General"
            });
        }

        // POST: /Expenses/samer
        [HttpPost("samer")]
        [HttpPost("/api/samer-expenses")]
        public async Task<IActionResult> CreateSamerExpense([FromBody] ExpenseApiRequest request)
        {
            if (request == null) return BadRequest(new { error = "Invalid payload" });

            try
            {
                if (!request.ReceiverId.HasValue)
                {
                    return BadRequest(new { error = "Valid receiver is required" });
                }

                var receiver = await _context.SamerExpenseReceivers.FindAsync(request.ReceiverId.Value);
                if (receiver == null)
                {
                    return BadRequest(new { error = "Valid receiver is required" });
                }

                DateTime date = DateTime.UtcNow;
                if (!string.IsNullOrEmpty(request.Date) && DateTime.TryParse(request.Date, out var parsedDate))
                {
                    date = parsedDate;
                }

                var expense = new SamerExpenses
                {
                    Date = date,
                    Amount = request.Amount,
                    Note = request.Note,
                    ReceiverId = receiver.Id,
                    DailyClosingId = request.DailyClosingId
                };

                _context.SamerExpenses.Add(expense);
                receiver.PaidAmount += request.Amount;

                await SyncDailyClosingTotalAsync(date, "total_expenses", request.Amount);
                await _context.SaveChangesAsync();

                // Add log event
                await LogEventAsync("SUCCESS", "samer_expense_create", $"Samer expense of {request.Amount} created successfully", $"{{\"id\": {expense.Id}, \"amount\": \"{request.Amount}\"}}");

                return Json(new { status = "success", message = "Samer expense created.", id = expense.Id });
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", error = ex.Message });
            }
        }

        // PUT: /Expenses/samer/5
        [HttpPut("samer/{id:int}")]
        [HttpPut("/api/samer-expenses/{id:int}")]
        public async Task<IActionResult> UpdateSamerExpense(int id, [FromBody] ExpenseApiRequest request)
        {
            if (request == null) return BadRequest(new { error = "Invalid payload" });

            try
            {
                var expense = await _context.SamerExpenses.FindAsync(id);
                if (expense == null) return NotFound();

                if (!request.ReceiverId.HasValue)
                {
                    return BadRequest(new { error = "Valid receiver is required" });
                }

                var receiver = await _context.SamerExpenseReceivers.FindAsync(request.ReceiverId.Value);
                if (receiver == null)
                {
                    return BadRequest(new { error = "Valid receiver is required" });
                }

                DateTime newDate = expense.Date;
                if (!string.IsNullOrEmpty(request.Date) && DateTime.TryParse(request.Date, out var parsedDate))
                {
                    newDate = parsedDate;
                }

                decimal oldAmount = expense.Amount;
                DateTime oldDate = expense.Date;

                // Sync daily closing
                if (oldDate.Date == newDate.Date)
                {
                    decimal diff = request.Amount - oldAmount;
                    if (diff != 0)
                    {
                        await SyncDailyClosingTotalAsync(oldDate, "total_expenses", diff);
                    }
                }
                else
                {
                    await SyncDailyClosingTotalAsync(oldDate, "total_expenses", -oldAmount);
                    await SyncDailyClosingTotalAsync(newDate, "total_expenses", request.Amount);
                }

                // Update Receiver paid amounts
                if (expense.ReceiverId != receiver.Id || expense.Amount != request.Amount)
                {
                    if (expense.ReceiverId.HasValue)
                    {
                        var oldRec = await _context.SamerExpenseReceivers.FindAsync(expense.ReceiverId.Value);
                        if (oldRec != null) oldRec.PaidAmount -= expense.Amount;
                    }
                    receiver.PaidAmount += request.Amount;
                }

                expense.Date = newDate;
                expense.Amount = request.Amount;
                expense.Note = request.Note;
                expense.ReceiverId = receiver.Id;
                expense.DailyClosingId = request.DailyClosingId;

                await _context.SaveChangesAsync();

                // Add log event
                await LogEventAsync("SUCCESS", "samer_expense_update", $"Samer expense of {request.Amount} updated successfully", $"{{\"id\": {expense.Id}, \"amount\": \"{request.Amount}\"}}");

                return Json(new { status = "success", message = "Samer expense updated." });
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", error = ex.Message });
            }
        }

        // DELETE: /Expenses/samer/5
        [HttpDelete("samer/{id:int}")]
        [HttpDelete("/api/samer-expenses/{id:int}")]
        public async Task<IActionResult> DeleteSamerExpense(int id)
        {
            try
            {
                var expense = await _context.SamerExpenses.FindAsync(id);
                if (expense == null) return NotFound();

                decimal amount = expense.Amount;
                DateTime date = expense.Date;

                await SyncDailyClosingTotalAsync(date, "total_expenses", -amount);

                if (expense.ReceiverId.HasValue)
                {
                    var receiver = await _context.SamerExpenseReceivers.FindAsync(expense.ReceiverId.Value);
                    if (receiver != null)
                    {
                        receiver.PaidAmount -= amount;
                    }
                }

                _context.SamerExpenses.Remove(expense);
                await _context.SaveChangesAsync();

                // Add log event
                await LogEventAsync("SUCCESS", "samer_expense_delete", $"Samer expense of {amount} deleted successfully", $"{{\"id\": {id}, \"amount\": \"{amount}\"}}");

                return Json(new { status = "success", ok = true, message = "Samer expense deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", error = ex.Message });
            }
        }

        // POST: /api/payroll/import
        [HttpPost("/api/payroll/import")]
        public async Task<IActionResult> ImportPayrollToExpenses([FromBody] PayrollImportRequest data)
        {
            if (data == null || string.IsNullOrEmpty(data.Type) || data.Month <= 0 || data.Year <= 0)
            {
                return BadRequest(new { error = "Missing required parameters: type, month, year" });
            }

            if (data.Type != "advances" && data.Type != "salaries")
            {
                return BadRequest(new { error = "Invalid import type. Use \"advances\" or \"salaries\"" });
            }

            try
            {
                // Calculate total sum
                var records = await _context.EmployeeWorkings
                    .Where(ew => ew.Month == data.Month && ew.Year == data.Year)
                    .ToListAsync();

                if (!records.Any())
                {
                    return NotFound(new { error = $"No payroll records found for {data.Month}/{data.Year}" });
                }

                decimal totalAmount = 0m;
                string receiverName = "";
                string note = "";

                if (data.Type == "advances")
                {
                    totalAmount = records.Sum(r => r.AdvanceTotal);
                    receiverName = "Staff Advances";
                    note = $"Imported total advances for {data.Month}/{data.Year}";
                }
                else
                {
                    totalAmount = records.Sum(r => r.Total);
                    receiverName = "Staff Salaries";
                    note = $"Imported total salaries for {data.Month}/{data.Year}";
                }

                if (totalAmount <= 0)
                {
                    return BadRequest(new { error = $"Total {data.Type} amount is zero for {data.Month}/{data.Year}" });
                }

                // Get or create receiver
                var receiver = await _context.Receivers.FirstOrDefaultAsync(r => r.Name == receiverName);
                if (receiver == null)
                {
                    receiver = new Receivers { Name = receiverName, PaidAmount = 0m };
                    _context.Receivers.Add(receiver);
                    await _context.SaveChangesAsync();
                }

                // Calculate last day of month
                int lastDay = DateTime.DaysInMonth(data.Year, data.Month);
                DateTime expenseDate = new DateTime(data.Year, data.Month, lastDay);

                // Check for duplicates
                var existing = await _context.Expenses.FirstOrDefaultAsync(e => e.ReceiverId == receiver.Id && e.Note == note);
                if (existing != null)
                {
                    return BadRequest(new { error = $"This {data.Type} import already exists for {data.Month}/{data.Year}" });
                }

                var expense = new Expenses
                {
                    Date = expenseDate,
                    Amount = totalAmount,
                    Note = note,
                    ReceiverId = receiver.Id
                };

                _context.Expenses.Add(expense);
                receiver.PaidAmount += totalAmount;

                await SyncDailyClosingTotalAsync(expenseDate, "total_expenses", totalAmount);
                await _context.SaveChangesAsync();

                await LogEventAsync("SUCCESS", "payroll_import", $"Imported {data.Type} as expense successfully", $"{{\"type\": \"{data.Type}\", \"amount\": \"{totalAmount}\"}}");

                return Json(new { status = "success", message = "Expense added" });
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", error = ex.Message });
            }
        }

        #region Helper Methods

        private async Task SyncDailyClosingTotalAsync(DateTime closeDate, string fieldName, decimal amountDiff)
        {
            var dateOnly = closeDate.Date;
            var closing = await _context.DailyClosings.FirstOrDefaultAsync(d => d.Date.Date == dateOnly);
            if (closing != null)
            {
                if (fieldName == "total_expenses")
                {
                    closing.TotalExpenses = closing.TotalExpenses + amountDiff;
                }
                else if (fieldName == "total_advance")
                {
                    closing.TotalAdvance = closing.TotalAdvance + amountDiff;
                }
                else if (fieldName == "total_credit")
                {
                    closing.TotalCredit = closing.TotalCredit + amountDiff;
                }
                else if (fieldName == "total_cashback")
                {
                    closing.TotalCashback = closing.TotalCashback + amountDiff;
                }
                else if (fieldName == "total_deductions")
                {
                    closing.TotalDeductions = closing.TotalDeductions + amountDiff;
                }

                closing.TotalCashout = closing.TotalExpenses + closing.TotalAdvance +
                                      closing.TotalDeductions + closing.TotalCredit +
                                      closing.TotalCashback;
                _context.DailyClosings.Update(closing);
            }
        }

        private async Task LogEventAsync(string level, string action, string message, string detailsJson)
        {
            try
            {
                var username = User.Identity?.Name;
                var user = username != null ? await _context.Users.FirstOrDefaultAsync(u => u.UserName == username) : null;
                int? userId = user?.Id;

                var log = new Logs
                {
                    CreatedAt = DateTime.UtcNow,
                    Level = level,
                    UserId = userId,
                    Username = username,
                    Action = action,
                    Message = message,
                    DetailsJson = detailsJson
                };

                _context.Logs.Add(log);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write event log to database");
            }
        }

        #endregion
    }

    #region Custom Request / DTO Classes

    public class ExpenseApiRequest
    {
        public string? Date { get; set; }
        public string? ReceiverName { get; set; }
        public int? ReceiverId { get; set; }
        public decimal Amount { get; set; }
        public string? Note { get; set; }
        public int? DailyClosingId { get; set; }
    }

    public class PayrollImportRequest
    {
        public string Type { get; set; } = string.Empty;
        public int Month { get; set; }
        public int Year { get; set; }
    }

    public class ExpensesViewModel
    {
        public List<Expenses> Expenses { get; set; } = new();
        public List<Receivers> Receivers { get; set; } = new();
        public ExpensesStats Stats { get; set; } = new();
        public int? CurrentReceiverId { get; set; }
        public int CurrentMonth { get; set; }
        public int CurrentYear { get; set; }
    }

    public class AhmadExpensesViewModel
    {
        public List<AhmadMistrahExpenses> Expenses { get; set; } = new();
        public List<AhmadExpenseReceivers> Receivers { get; set; } = new();
        public ExpensesStats Stats { get; set; } = new();
        public int? ReceiverId { get; set; }
        public int CurrentMonth { get; set; }
        public int CurrentYear { get; set; }
    }

    public class SamerExpensesViewModel
    {
        public List<SamerExpenses> Expenses { get; set; } = new();
        public List<SamerExpenseReceivers> Receivers { get; set; } = new();
        public ExpensesStats Stats { get; set; } = new();
        public int? ReceiverId { get; set; }
        public int CurrentMonth { get; set; }
        public int CurrentYear { get; set; }
    }

    public class ExpensesStats
    {
        public decimal Total { get; set; }
        public decimal Max { get; set; }
        public int Count { get; set; }
        public string TopReceiver { get; set; } = "N/A";
        public decimal TopReceiverAmount { get; set; }
    }

    #endregion
}

