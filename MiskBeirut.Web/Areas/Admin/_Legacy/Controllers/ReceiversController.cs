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
    public class ReceiversController : Controller
    {
        private readonly IReceiversService _receiversService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReceiversController> _logger;
        private readonly IAuditLogService _auditLogService;

        public ReceiversController(IReceiversService receiversService, ApplicationDbContext context, ILogger<ReceiversController> logger, IAuditLogService auditLogService)
        {
            _receiversService = receiversService;
            _context = context;
            _logger = logger;
            _auditLogService = auditLogService;
        }

        #region Standard Receivers CRUD

        // GET: /Receivers
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Retrieving all Receivers");
            var receivers = await _receiversService.GetReceiversAsync();
            return View(receivers);
        }

        // GET: /Receivers/5 or /api/Receivers/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetReceiver(int id)
        {
            _logger.LogInformation("Retrieving Receivers with ID {ResourceId}", id);
            var receiver = await _receiversService.GetReceiverAsync(id);
            if (receiver == null)
            {
                _logger.LogWarning("Receivers with ID {ResourceId} was not found.", id);
                return NotFound();
            }
            return Json(receiver);
        }

        // POST: /Receivers or /api/Receivers
        [HttpPost("/api/receivers")]
        public async Task<IActionResult> CreateReceiver([FromBody] ReceiversDto receiverDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Create Receivers request failed validation");
                return Json(new { status = "error", message = "Invalid receiver details." });
            }

            try
            {
                _logger.LogInformation("Creating new Receivers with data: {@RequestData}", receiverDto);
                var created = await _receiversService.CreateReceiverAsync(receiverDto);
                _logger.LogInformation("Successfully created Receivers with ID: {ResourceId}", created.Id);

                // Audit log
                try
                {
                    var newValues = new { created.Name, created.PaidAmount };
                    var username = User?.Identity?.Name ?? "Unknown";
                    var userId = (await _context.Users.FirstOrDefaultAsync(u => u.UserName == username))?.Id;
                    var ipAddress = HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
                    await _auditLogService.LogAddAsync("Receiver", created.Id, JsonSerializer.Serialize(newValues), username, userId, ipAddress, $"Receiver created: {created.Name}");
                }
                catch (Exception auditEx)
                {
                    _logger.LogError(auditEx, "Failed to create audit log for Receiver ID: {ResourceId}", created.Id);
                }

                return Json(new { status = "success", message = "Receiver created successfully.", id = created.Id, name = created.Name });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute CreateReceiver on Receivers");
                return Json(new { status = "error", message = ex.Message });
            }
        }

        // PUT: /Receivers/5 or /api/Receivers/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateReceiver(int id, [FromBody] ReceiversDto receiverDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Update Receivers request failed validation for ID {ResourceId}", id);
                return Json(new { status = "error", message = "Invalid receiver details." });
            }

            try
            {
                _logger.LogInformation("Updating Receivers ID {ResourceId}", id);
                var oldReceiver = await _context.Receivers.FindAsync(id);
                if (oldReceiver == null)
                {
                    _logger.LogWarning("Receivers with ID {ResourceId} was not found.", id);
                    return NotFound();
                }

                var oldValues = new { oldReceiver.Name, oldReceiver.PaidAmount };

                var success = await _receiversService.UpdateReceiverAsync(id, receiverDto);
                if (!success)
                {
                    _logger.LogWarning("Receivers with ID {ResourceId} was not found.", id);
                    return NotFound();
                }
                _logger.LogInformation("Successfully updated Receivers ID {ResourceId}", id);

                // Audit log
                try
                {
                    var newValues = new { receiverDto.Name, receiverDto.PaidAmount };
                    var username = User?.Identity?.Name ?? "Unknown";
                    var userId = (await _context.Users.FirstOrDefaultAsync(u => u.UserName == username))?.Id;
                    var ipAddress = HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
                    await _auditLogService.LogUpdateAsync("Receiver", id, JsonSerializer.Serialize(oldValues), JsonSerializer.Serialize(newValues), username, userId, ipAddress, $"Receiver updated: {receiverDto.Name}");
                }
                catch (Exception auditEx)
                {
                    _logger.LogError(auditEx, "Failed to create audit log for Receiver ID: {ResourceId}", id);
                }

                return Json(new { status = "success", message = "Receiver updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute UpdateReceiver on Receivers for ID {ResourceId}", id);
                return Json(new { status = "error", message = ex.Message });
            }
        }

        // DELETE: /Receivers/5 or /api/Receivers/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteReceiver(int id)
        {
            try
            {
                _logger.LogWarning("Initiating deletion of Receivers ID {ResourceId}", id);
                var receiver = await _context.Receivers.FindAsync(id);
                if (receiver == null)
                {
                    _logger.LogWarning("Receivers with ID {ResourceId} was not found.", id);
                    return NotFound();
                }

                var oldValues = new { receiver.Name, receiver.PaidAmount };

                var success = await _receiversService.DeleteReceiverAsync(id);
                if (!success)
                {
                    _logger.LogWarning("Receivers with ID {ResourceId} was not found.", id);
                    return NotFound();
                }
                _logger.LogInformation("Successfully deleted Receivers ID {ResourceId}", id);

                // Audit log
                try
                {
                    var username = User?.Identity?.Name ?? "Unknown";
                    var userId = (await _context.Users.FirstOrDefaultAsync(u => u.UserName == username))?.Id;
                    var ipAddress = HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
                    await _auditLogService.LogDeleteAsync("Receiver", id, JsonSerializer.Serialize(oldValues), username, userId, ipAddress, $"Receiver deleted: {receiver.Name}");
                }
                catch (Exception auditEx)
                {
                    _logger.LogError(auditEx, "Failed to create audit log for Receiver ID: {ResourceId}", id);
                }

                return Json(new { status = "success", message = "Receiver deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute DeleteReceiver on Receivers for ID {ResourceId}", id);
                return Json(new { status = "error", message = ex.Message });
            }
        }

        #endregion

        #region Ahmad Expense Receivers CRUD

        // GET: /Receivers/ahmad or /api/Receivers/ahmad
        [HttpGet("ahmad")]
        public async Task<IActionResult> GetAhmadReceivers()
        {
            _logger.LogInformation("Retrieving all AhmadExpenseReceivers");
            var receivers = await _receiversService.GetAhmadReceiversAsync();
            if (Request.Path.Value?.Contains("/api/") == true)
            {
                return Json(receivers);
            }
            return View("Ahmad", receivers);
        }

        // GET: /Receivers/ahmad/5 or /api/Receivers/ahmad/5
        [HttpGet("ahmad/{id:int}")]
        public async Task<IActionResult> GetAhmadReceiver(int id)
        {
            _logger.LogInformation("Retrieving AhmadExpenseReceivers with ID {ResourceId}", id);
            var receiver = await _receiversService.GetAhmadReceiverAsync(id);
            if (receiver == null)
            {
                _logger.LogWarning("AhmadExpenseReceivers with ID {ResourceId} was not found.", id);
                return NotFound();
            }
            return Json(receiver);
        }

        // POST: /Receivers/ahmad or /api/Receivers/ahmad
        [HttpPost("ahmad")]
        public async Task<IActionResult> CreateAhmadReceiver([FromBody] AhmadExpenseReceiversDto receiverDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Create AhmadExpenseReceivers request failed validation");
                return Json(new { status = "error", message = "Invalid receiver details." });
            }

            try
            {
                _logger.LogInformation("Creating new AhmadExpenseReceivers with data: {@RequestData}", receiverDto);
                var created = await _receiversService.CreateAhmadReceiverAsync(receiverDto);
                _logger.LogInformation("Successfully created AhmadExpenseReceivers with ID: {ResourceId}", created.Id);
                return Json(new { status = "success", message = "Receiver created successfully.", id = created.Id, name = created.Name });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute CreateAhmadReceiver on AhmadExpenseReceivers");
                return Json(new { status = "error", message = ex.Message });
            }
        }

        // PUT: /Receivers/ahmad/5 or /api/Receivers/ahmad/5
        [HttpPut("ahmad/{id:int}")]
        public async Task<IActionResult> UpdateAhmadReceiver(int id, [FromBody] AhmadExpenseReceiversDto receiverDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Update AhmadExpenseReceivers request failed validation for ID {ResourceId}", id);
                return Json(new { status = "error", message = "Invalid receiver details." });
            }

            try
            {
                _logger.LogInformation("Updating AhmadExpenseReceivers ID {ResourceId}", id);
                var success = await _receiversService.UpdateAhmadReceiverAsync(id, receiverDto);
                if (!success)
                {
                    _logger.LogWarning("AhmadExpenseReceivers with ID {ResourceId} was not found.", id);
                    return NotFound();
                }
                _logger.LogInformation("Successfully updated AhmadExpenseReceivers ID {ResourceId}", id);
                return Json(new { status = "success", message = "Receiver updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute UpdateAhmadReceiver on AhmadExpenseReceivers for ID {ResourceId}", id);
                return Json(new { status = "error", message = ex.Message });
            }
        }

        // DELETE: /Receivers/ahmad/5 or /api/Receivers/ahmad/5
        [HttpDelete("ahmad/{id:int}")]
        public async Task<IActionResult> DeleteAhmadReceiver(int id)
        {
            try
            {
                _logger.LogWarning("Initiating deletion of AhmadExpenseReceivers ID {ResourceId}", id);
                var success = await _receiversService.DeleteAhmadReceiverAsync(id);
                if (!success)
                {
                    _logger.LogWarning("AhmadExpenseReceivers with ID {ResourceId} was not found.", id);
                    return NotFound();
                }
                _logger.LogInformation("Successfully deleted AhmadExpenseReceivers ID {ResourceId}", id);
                return Json(new { status = "success", message = "Receiver deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute DeleteAhmadReceiver on AhmadExpenseReceivers for ID {ResourceId}", id);
                return Json(new { status = "error", message = ex.Message });
            }
        }

        #endregion

        #region Samer Expense Receivers CRUD

        // GET: /Receivers/samer or /api/Receivers/samer
        [HttpGet("samer")]
        public async Task<IActionResult> GetSamerReceivers()
        {
            _logger.LogInformation("Retrieving all SamerExpenseReceivers");
            var receivers = await _receiversService.GetSamerReceiversAsync();
            if (Request.Path.Value?.Contains("/api/") == true)
            {
                return Json(receivers);
            }
            return View("Samer", receivers);
        }

        // GET: /Receivers/samer/5 or /api/Receivers/samer/5
        [HttpGet("samer/{id:int}")]
        public async Task<IActionResult> GetSamerReceiver(int id)
        {
            _logger.LogInformation("Retrieving SamerExpenseReceivers with ID {ResourceId}", id);
            var receiver = await _receiversService.GetSamerReceiverAsync(id);
            if (receiver == null)
            {
                _logger.LogWarning("SamerExpenseReceivers with ID {ResourceId} was not found.", id);
                return NotFound();
            }
            return Json(receiver);
        }

        // POST: /Receivers/samer or /api/Receivers/samer
        [HttpPost("samer")]
        public async Task<IActionResult> CreateSamerReceiver([FromBody] SamerExpenseReceiversDto receiverDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Create SamerExpenseReceivers request failed validation");
                return Json(new { status = "error", message = "Invalid receiver details." });
            }

            try
            {
                _logger.LogInformation("Creating new SamerExpenseReceivers with data: {@RequestData}", receiverDto);
                var created = await _receiversService.CreateSamerReceiverAsync(receiverDto);
                _logger.LogInformation("Successfully created SamerExpenseReceivers with ID: {ResourceId}", created.Id);
                return Json(new { status = "success", message = "Receiver created successfully.", id = created.Id, name = created.Name });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute CreateSamerReceiver on SamerExpenseReceivers");
                return Json(new { status = "error", message = ex.Message });
            }
        }

        // PUT: /Receivers/samer/5 or /api/Receivers/samer/5
        [HttpPut("samer/{id:int}")]
        public async Task<IActionResult> UpdateSamerReceiver(int id, [FromBody] SamerExpenseReceiversDto receiverDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Update SamerExpenseReceivers request failed validation for ID {ResourceId}", id);
                return Json(new { status = "error", message = "Invalid receiver details." });
            }

            try
            {
                _logger.LogInformation("Updating SamerExpenseReceivers ID {ResourceId}", id);
                var success = await _receiversService.UpdateSamerReceiverAsync(id, receiverDto);
                if (!success)
                {
                    _logger.LogWarning("SamerExpenseReceivers with ID {ResourceId} was not found.", id);
                    return NotFound();
                }
                _logger.LogInformation("Successfully updated SamerExpenseReceivers ID {ResourceId}", id);
                return Json(new { status = "success", message = "Receiver updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute UpdateSamerReceiver on SamerExpenseReceivers for ID {ResourceId}", id);
                return Json(new { status = "error", message = ex.Message });
            }
        }

        // DELETE: /Receivers/samer/5 or /api/Receivers/samer/5
        [HttpDelete("samer/{id:int}")]
        [HttpDelete("/api/samer-receivers/{id:int}")]
        public async Task<IActionResult> DeleteSamerReceiver(int id)
        {
            try
            {
                _logger.LogWarning("Initiating deletion of SamerExpenseReceivers ID {ResourceId}", id);
                var success = await _receiversService.DeleteSamerReceiverAsync(id);
                if (!success)
                {
                    _logger.LogWarning("SamerExpenseReceivers with ID {ResourceId} was not found.", id);
                    return NotFound();
                }
                _logger.LogInformation("Successfully deleted SamerExpenseReceivers ID {ResourceId}", id);
                return Json(new { status = "success", message = "Receiver deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute DeleteSamerReceiver on SamerExpenseReceivers for ID {ResourceId}", id);
                return Json(new { status = "error", message = ex.Message });
            }
        }

        #endregion

        #region Expense History

        // GET: /api/Receivers/{receiverId}/expenses?from=&to=
        [HttpGet("{receiverId:int}/expenses")]
        public async Task<IActionResult> GetReceiverExpenses(int receiverId, [FromQuery] string? from, [FromQuery] string? to)
        {
            if (!DateTime.TryParse(from, out var fromDate) || !DateTime.TryParse(to, out var toDate))
                return Json(new { status = "error", message = "Please provide valid from and to dates." });

            toDate = toDate.Date.AddDays(1).AddTicks(-1);

            var expenses = await _context.Expenses
                .Where(e => e.ReceiverId == receiverId && e.Date >= fromDate && e.Date <= toDate)
                .OrderByDescending(e => e.Date)
                .Select(e => new { e.Id, date = e.Date.ToString("yyyy-MM-dd"), e.Amount, e.Note })
                .ToListAsync();

            return Json(new
            {
                status = "success",
                expenses,
                total = expenses.Sum(e => e.Amount)
            });
        }

        #endregion

        #region Routing Compatibility for script.js

        // GET: /api/receivers/list
        [HttpGet("/api/receivers/list")]
        public async Task<IActionResult> GetReceiversList()
        {
            var list = await _receiversService.GetReceiversAsync();
            return Json(list.Select(r => new { id = r.Id, name = r.Name }));
        }

        // GET: /api/samer-receivers/list
        [HttpGet("/api/samer-receivers/list")]
        public async Task<IActionResult> GetSamerReceiversList()
        {
            var list = await _receiversService.GetSamerReceiversAsync();
            return Json(list.Select(r => new { id = r.Id, name = r.Name }));
        }

        // GET: /api/ahmad-receivers/list
        [HttpGet("/api/ahmad-receivers/list")]
        public async Task<IActionResult> GetAhmadReceiversList()
        {
            var list = await _receiversService.GetAhmadReceiversAsync();
            return Json(list.Select(r => new { id = r.Id, name = r.Name }));
        }

        // GET: /api/suggestions/receivers
        [HttpGet("/api/suggestions/receivers")]
        public async Task<IActionResult> GetReceiverSuggestions()
        {
            var list = await _receiversService.GetReceiversAsync();
            return Json(list.Select(r => r.Name).Distinct().ToList());
        }

        // GET: /api/suggestions/samer-receivers
        [HttpGet("/api/suggestions/samer-receivers")]
        public async Task<IActionResult> GetSamerReceiverSuggestions()
        {
            var list = await _receiversService.GetSamerReceiversAsync();
            return Json(list.Select(r => r.Name).Distinct().ToList());
        }

        // GET: /api/ahmad-receivers/suggestions
        [HttpGet("/api/ahmad-receivers/suggestions")]
        public async Task<IActionResult> GetAhmadReceiverSuggestions()
        {
            var list = await _receiversService.GetAhmadReceiversAsync();
            return Json(list.Select(r => r.Name).Distinct().ToList());
        }

        // POST: /api/ahmad-receivers
        [HttpPost("/api/ahmad-receivers")]
        public async Task<IActionResult> CreateAhmadReceiverPost([FromBody] AhmadExpenseReceiversDto dto)
        {
            return await CreateAhmadReceiver(dto);
        }

        // POST: /api/samer-receivers
        [HttpPost("/api/samer-receivers")]
        public async Task<IActionResult> CreateSamerReceiverPost([FromBody] SamerExpenseReceiversDto dto)
        {
            return await CreateSamerReceiver(dto);
        }

        // PUT: /api/samer-receivers/{id}
        [HttpPut("/api/samer-receivers/{id:int}")]
        public async Task<IActionResult> UpdateSamerReceiverPut(int id, [FromBody] SamerExpenseReceiversDto dto)
        {
            return await UpdateSamerReceiver(id, dto);
        }

        // PUT: /api/ahmad-receivers/{id}
        [HttpPut("/api/ahmad-receivers/{id:int}")]
        public async Task<IActionResult> UpdateAhmadReceiverPut(int id, [FromBody] AhmadExpenseReceiversDto dto)
        {
            return await UpdateAhmadReceiver(id, dto);
        }

        // DELETE: /api/ahmad-receivers/{id}
        [HttpDelete("/api/ahmad-receivers/{id:int}")]
        public async Task<IActionResult> DeleteAhmadReceiverDelete(int id)
        {
            return await DeleteAhmadReceiver(id);
        }

        #endregion
    }
}
