using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiskBeirut.Application.DTOs;
using MiskBeirut.Application.Services;
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
    public class CustomersController : Controller
    {
        private readonly ICustomerService _customerService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CustomersController> _logger;
        private readonly IAuditLogService _auditLogService;

        public CustomersController(ICustomerService customerService, ApplicationDbContext context, ILogger<CustomersController> logger, IAuditLogService auditLogService)
        {
            _customerService = customerService;
            _context = context;
            _logger = logger;
            _auditLogService = auditLogService;
        }

        #region Customers CRUD

        // GET: /Customers
        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] string? start_date, [FromQuery] string? end_date)
        {
            _logger.LogInformation("Retrieving Customers list. Start Date: {StartDate}, End Date: {EndDate}", start_date, start_date);
            var customersList = await _customerService.GetCustomersAsync();
            var customers = customersList.ToList();

            ViewData["StartDate"] = start_date;
            ViewData["EndDate"] = end_date;

            if (!string.IsNullOrEmpty(start_date) && !string.IsNullOrEmpty(end_date))
            {
                if (DateTime.TryParse(start_date, out var startDate) && DateTime.TryParse(end_date, out var endDate))
                {
                    endDate = endDate.Date.AddDays(1).AddTicks(-1); // End of day

                    for (int i = 0; i < customers.Count; i++)
                    {
                        var cust = customers[i];
                        decimal creditSum = await _context.Credits
                            .Where(c => c.CustomerId == cust.Id && c.Date >= startDate && c.Date <= endDate)
                            .SumAsync(c => c.Amount);

                        decimal cashbackSum = await _context.Cashbacks
                            .Where(cb => cb.CustomerId == cust.Id && cb.Date >= startDate && cb.Date <= endDate)
                            .SumAsync(cb => cb.Amount);

                        customers[i] = cust with { Balance = cashbackSum - creditSum };
                    }
                }
            }

            return View(customers);
        }

        // GET: /Customers/5 or /api/customers/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetCustomer(int id)
        {
            _logger.LogInformation("Retrieving Customer with ID: {ResourceId}", id);
            var customer = await _customerService.GetCustomerAsync(id);
            if (customer == null)
            {
                _logger.LogWarning("Customer with ID {ResourceId} was not found.", id);
                return NotFound();
            }
            return Json(customer);
        }

        // POST: /Customers or /api/customers
        [HttpPost]
        public async Task<IActionResult> CreateCustomer([FromBody] CustomersDto customerDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for CreateCustomer request.");
                return Json(new { status = "error", message = "Invalid customer details." });
            }

            try
            {
                _logger.LogInformation("Creating new Customer with data: {@RequestData}", customerDto);
                var created = await _customerService.CreateCustomerAsync(customerDto);
                _logger.LogInformation("Successfully created Customer with ID: {ResourceId}", created.Id);

                // Audit log
                try
                {
                    var newValues = new { created.Username, created.Balance };
                    var username = User?.Identity?.Name ?? "Unknown";
                    var userId = (await _context.Users.FirstOrDefaultAsync(u => u.UserName == username))?.Id;
                    var ipAddress = HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
                    await _auditLogService.LogAddAsync("Customer", created.Id, JsonSerializer.Serialize(newValues), username, userId, ipAddress, $"Customer created: {created.Username}");
                }
                catch (Exception auditEx)
                {
                    _logger.LogError(auditEx, "Failed to create audit log for Customer ID: {ResourceId}", created.Id);
                }

                return Json(new { status = "success", message = "Customer created successfully.", id = created.Id, name = created.Username });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create Customer");
                return Json(new { status = "error", message = ex.Message });
            }
        }

        // PUT: /Customers/5 or /api/customers/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateCustomer(int id, [FromBody] CustomersDto customerDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for UpdateCustomer request ID {ResourceId}.", id);
                return Json(new { status = "error", message = "Invalid customer details." });
            }

            try
            {
                _logger.LogInformation("Updating Customer ID {ResourceId}", id);
                var oldCustomer = await _context.Customers.FindAsync(id);
                if (oldCustomer == null)
                {
                    _logger.LogWarning("Customer with ID {ResourceId} was not found for update.", id);
                    return NotFound();
                }

                var oldValues = new { oldCustomer.Username, oldCustomer.Balance };

                var success = await _customerService.UpdateCustomerAsync(id, customerDto);
                if (!success)
                {
                    _logger.LogWarning("Customer with ID {ResourceId} was not found for update.", id);
                    return NotFound();
                }
                _logger.LogInformation("Successfully updated Customer ID {ResourceId}", id);

                // Audit log
                try
                {
                    var newValues = new { customerDto.Username, customerDto.Balance };
                    var username = User?.Identity?.Name ?? "Unknown";
                    var userId = (await _context.Users.FirstOrDefaultAsync(u => u.UserName == username))?.Id;
                    var ipAddress = HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
                    await _auditLogService.LogUpdateAsync("Customer", id, JsonSerializer.Serialize(oldValues), JsonSerializer.Serialize(newValues), username, userId, ipAddress, $"Customer updated: {customerDto.Username}");
                }
                catch (Exception auditEx)
                {
                    _logger.LogError(auditEx, "Failed to create audit log for Customer ID: {ResourceId}", id);
                }

                return Json(new { status = "success", message = "Customer updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update Customer ID {ResourceId}", id);
                return Json(new { status = "error", message = ex.Message });
            }
        }

        // DELETE: /Customers/5 or /api/customers/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            try
            {
                _logger.LogWarning("Initiating deletion of Customer ID {ResourceId}", id);
                var customer = await _context.Customers.FindAsync(id);
                if (customer == null)
                {
                    _logger.LogWarning("Customer with ID {ResourceId} was not found for deletion.", id);
                    return NotFound();
                }

                var oldValues = new { customer.Username, customer.Balance };

                var success = await _customerService.DeleteCustomerAsync(id);
                if (!success)
                {
                    _logger.LogWarning("Customer with ID {ResourceId} was not found for deletion.", id);
                    return NotFound();
                }
                _logger.LogInformation("Successfully deleted Customer ID {ResourceId}", id);

                // Audit log
                try
                {
                    var username = User?.Identity?.Name ?? "Unknown";
                    var userId = (await _context.Users.FirstOrDefaultAsync(u => u.UserName == username))?.Id;
                    var ipAddress = HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
                    await _auditLogService.LogDeleteAsync("Customer", id, JsonSerializer.Serialize(oldValues), username, userId, ipAddress, $"Customer deleted: {customer.Username}");
                }
                catch (Exception auditEx)
                {
                    _logger.LogError(auditEx, "Failed to create audit log for Customer ID: {ResourceId}", id);
                }

                return Json(new { status = "success", message = "Customer deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete Customer ID {ResourceId}", id);
                return Json(new { status = "error", message = ex.Message });
            }
        }

        #endregion

        #region Credits CRUD (Customer Sales)

        // GET: /control-panel/credits
        [HttpGet("/control-panel/credits")]
        public async Task<IActionResult> Credits([FromQuery] int? month, [FromQuery] int? year)
        {
            var now = DateTime.UtcNow;
            int selectedMonth = month ?? now.Month;
            int selectedYear = year ?? now.Year;

            var query = _context.Credits.Include(c => c.Customer).AsQueryable();

            if (selectedMonth > 0)
            {
                query = query.Where(c => c.Date.Month == selectedMonth);
            }
            if (selectedYear > 0)
            {
                query = query.Where(c => c.Date.Year == selectedYear);
            }

            var records = await query.OrderByDescending(c => c.Date).ToListAsync();

            decimal total = records.Sum(r => r.Amount);
            int count = records.Count;
            decimal max = records.Any() ? records.Max(r => r.Amount) : 0;

            ViewData["CurrentMonth"] = selectedMonth;
            ViewData["CurrentYear"] = selectedYear;
            ViewData["Total"] = total;
            ViewData["Count"] = count;
            ViewData["Max"] = max;

            return View(records);
        }

        // GET: /Customers/5/credits or /api/customers/5/credits
        [HttpGet("{customerId:int}/credits")]
        public async Task<IActionResult> GetCustomerCredits(int customerId)
        {
            var credits = await _customerService.GetCustomerCreditsAsync(customerId);
            return Json(credits);
        }

        // POST: /Customers/credits or /api/customers/credits
        [HttpPost("credits")]
        public async Task<IActionResult> CreateCredit([FromBody] CreditsDto creditDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for CreateCredit request.");
                return Json(new { status = "error", message = "Invalid credit details." });
            }

            try
            {
                _logger.LogInformation("Creating credit record for Customer ID {CustomerId} with amount: {Amount}", creditDto.CustomerId, creditDto.Amount);
                var created = await _customerService.CreateCreditAsync(creditDto);
                _logger.LogInformation("Successfully created credit record ID {ResourceId} for Customer ID {CustomerId}", created.Id, creditDto.CustomerId);
                return Json(new { status = "success", message = "Credit record created successfully.", id = created.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create credit record for Customer ID {CustomerId}", creditDto.CustomerId);
                return Json(new { status = "error", message = ex.Message });
            }
        }

        // DELETE: /Customers/credits/5 or /api/customers/credits/5
        [HttpDelete("credits/{id:int}")]
        public async Task<IActionResult> DeleteCredit(int id)
        {
            try
            {
                _logger.LogWarning("Initiating deletion of credit record ID {ResourceId}", id);
                var success = await _customerService.DeleteCreditAsync(id);
                if (!success)
                {
                    _logger.LogWarning("Credit record ID {ResourceId} was not found for deletion.", id);
                    return NotFound();
                }
                _logger.LogInformation("Successfully deleted credit record ID {ResourceId}", id);
                return Json(new { status = "success", message = "Credit record deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete credit record ID {ResourceId}", id);
                return Json(new { status = "error", message = ex.Message });
            }
        }

        #endregion

        #region Cashbacks CRUD

        // GET: /control-panel/cashbacks
        [HttpGet("/control-panel/cashbacks")]
        public async Task<IActionResult> Cashbacks([FromQuery] int? month, [FromQuery] int? year)
        {
            var now = DateTime.UtcNow;
            int selectedMonth = month ?? now.Month;
            int selectedYear = year ?? now.Year;

            var query = _context.Cashbacks.Include(c => c.Customer).AsQueryable();

            if (selectedMonth > 0)
            {
                query = query.Where(c => c.Date.Month == selectedMonth);
            }
            if (selectedYear > 0)
            {
                query = query.Where(c => c.Date.Year == selectedYear);
            }

            var records = await query.OrderByDescending(c => c.Date).ToListAsync();

            decimal total = records.Sum(r => r.Amount);
            int count = records.Count;
            decimal max = records.Any() ? records.Max(r => r.Amount) : 0;

            ViewData["CurrentMonth"] = selectedMonth;
            ViewData["CurrentYear"] = selectedYear;
            ViewData["Total"] = total;
            ViewData["Count"] = count;
            ViewData["Max"] = max;

            return View(records);
        }

        // GET: /Customers/5/cashbacks or /api/customers/5/cashbacks
        [HttpGet("{customerId:int}/cashbacks")]
        public async Task<IActionResult> GetCustomerCashbacks(int customerId)
        {
            var cashbacks = await _customerService.GetCustomerCashbacksAsync(customerId);
            return Json(cashbacks);
        }

        // POST: /Customers/cashbacks or /api/customers/cashbacks
        [HttpPost("cashbacks")]
        public async Task<IActionResult> CreateCashback([FromBody] CashbacksDto cashbackDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for CreateCashback request.");
                return Json(new { status = "error", message = "Invalid cashback details." });
            }

            try
            {
                _logger.LogInformation("Creating cashback record for Customer ID {CustomerId} with amount: {Amount}", cashbackDto.CustomerId, cashbackDto.Amount);
                var created = await _customerService.CreateCashbackAsync(cashbackDto);
                _logger.LogInformation("Successfully created cashback record ID {ResourceId} for Customer ID {CustomerId}", created.Id, cashbackDto.CustomerId);
                return Json(new { status = "success", message = "Cashback record created successfully.", id = created.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create cashback record for Customer ID {CustomerId}", cashbackDto.CustomerId);
                return Json(new { status = "error", message = ex.Message });
            }
        }

        // DELETE: /Customers/cashbacks/5 or /api/customers/cashbacks/5
        [HttpDelete("cashbacks/{id:int}")]
        public async Task<IActionResult> DeleteCashback(int id)
        {
            try
            {
                _logger.LogWarning("Initiating deletion of cashback record ID {ResourceId}", id);
                var success = await _customerService.DeleteCashbackAsync(id);
                if (!success)
                {
                    _logger.LogWarning("Cashback record ID {ResourceId} was not found for deletion.", id);
                    return NotFound();
                }
                _logger.LogInformation("Successfully deleted cashback record ID {ResourceId}", id);
                return Json(new { status = "success", message = "Cashback record deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete cashback record ID {ResourceId}", id);
                return Json(new { status = "error", message = ex.Message });
            }
        }

        // GET: /api/customers/{customerId}/history?from=&to=
        [HttpGet("{customerId:int}/history")]
        public async Task<IActionResult> GetCustomerHistory(int customerId, [FromQuery] string? from, [FromQuery] string? to)
        {
            if (!DateTime.TryParse(from, out var fromDate) || !DateTime.TryParse(to, out var toDate))
                return Json(new { status = "error", message = "Please provide valid from and to dates." });

            toDate = toDate.Date.AddDays(1).AddTicks(-1);

            var credits = await _context.Credits
                .Where(c => c.CustomerId == customerId && c.Date >= fromDate && c.Date <= toDate)
                .OrderByDescending(c => c.Date)
                .Select(c => new { c.Id, date = c.Date.ToString("yyyy-MM-dd"), c.Amount, c.Note })
                .ToListAsync();

            var cashbacks = await _context.Cashbacks
                .Where(c => c.CustomerId == customerId && c.Date >= fromDate && c.Date <= toDate)
                .OrderByDescending(c => c.Date)
                .Select(c => new { c.Id, date = c.Date.ToString("yyyy-MM-dd"), c.Amount, c.Note })
                .ToListAsync();

            return Json(new
            {
                status = "success",
                credits,
                cashbacks,
                totalCredits = credits.Sum(c => c.Amount),
                totalCashbacks = cashbacks.Sum(c => c.Amount)
            });
        }

        // GET: /api/customers/list
        [HttpGet("/api/customers/list")]
        public async Task<IActionResult> GetCustomersList()
        {
            var list = await _customerService.GetCustomersAsync();
            return Json(list.Select(c => new { id = c.Id, name = c.Username }));
        }

        // GET: /api/suggestions/customers
        [HttpGet("/api/suggestions/customers")]
        public async Task<IActionResult> GetCustomerSuggestions()
        {
            var list = await _customerService.GetCustomersAsync();
            return Json(list.Select(c => c.Username).Distinct().ToList());
        }

        #endregion
    }
}
