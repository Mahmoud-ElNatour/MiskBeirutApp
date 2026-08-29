using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiskBeirut.Core.Interfaces;
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
    public class AuditLogsController : Controller
    {
        private readonly IAuditLogService _auditLogService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuditLogsController> _logger;

        public AuditLogsController(IAuditLogService auditLogService, ApplicationDbContext context, ILogger<AuditLogsController> logger)
        {
            _auditLogService = auditLogService;
            _context = context;
            _logger = logger;
        }

        // GET: /control-panel/audit-logs
        [HttpGet("/control-panel/audit-logs")]
        public async Task<IActionResult> Index([FromQuery] string? entityType, [FromQuery] string? action, [FromQuery] int? entityId, [FromQuery] string? fromDate, [FromQuery] string? toDate)
        {
            _logger.LogInformation("Retrieving audit logs with filters - EntityType: {EntityType}, Action: {Action}, EntityId: {EntityId}", entityType, action, entityId);

            DateTime? fromDateTime = null;
            DateTime? toDateTime = null;

            if (!string.IsNullOrEmpty(fromDate) && DateTime.TryParse(fromDate, out var parsedFrom))
            {
                fromDateTime = parsedFrom;
            }

            if (!string.IsNullOrEmpty(toDate) && DateTime.TryParse(toDate, out var parsedTo))
            {
                toDateTime = parsedTo.Date.AddDays(1).AddTicks(-1);
            }

            var logs = await _auditLogService.GetAuditLogsAsync(
                string.IsNullOrEmpty(entityType) ? null : entityType,
                string.IsNullOrEmpty(action) ? null : action,
                entityId,
                fromDateTime,
                toDateTime
            );

            ViewData["SelectedEntityType"] = entityType ?? "";
            ViewData["SelectedAction"] = action ?? "";
            ViewData["SelectedEntityId"] = entityId?.ToString() ?? "";
            ViewData["SelectedFromDate"] = fromDate ?? "";
            ViewData["SelectedToDate"] = toDate ?? "";

            return View(logs);
        }

        // GET: /api/audit-logs
        [HttpGet("/api/audit-logs")]
        public async Task<IActionResult> GetAuditLogs([FromQuery] string? entityType, [FromQuery] string? action, [FromQuery] int? entityId, [FromQuery] int? days = 30)
        {
            _logger.LogInformation("Retrieving recent audit logs (last {Days} days)", days ?? 30);
            var logs = await _auditLogService.GetRecentAuditLogsAsync(days ?? 30);

            if (!string.IsNullOrEmpty(entityType))
            {
                logs = logs.Where(l => l.EntityType == entityType).ToList();
            }

            if (!string.IsNullOrEmpty(action))
            {
                logs = logs.Where(l => l.Action == action).ToList();
            }

            if (entityId.HasValue)
            {
                logs = logs.Where(l => l.EntityId == entityId.Value).ToList();
            }

            return Json(new { status = "success", data = logs });
        }

        // GET: /api/audit-logs/entity/{entityType}/{entityId}
        [HttpGet("/api/audit-logs/entity/{entityType}/{entityId:int}")]
        public async Task<IActionResult> GetEntityHistory(string entityType, int entityId)
        {
            _logger.LogInformation("Retrieving audit history for {EntityType} ID {EntityId}", entityType, entityId);
            var history = await _auditLogService.GetEntityAuditHistoryAsync(entityType, entityId);

            return Json(new { status = "success", data = history });
        }

        // GET: /audit-logs/detail/{id}
        [HttpGet("detail/{id:int}")]
        public async Task<IActionResult> ViewDetails(int id)
        {
            var log = await _context.AuditLogs.FirstOrDefaultAsync(l => l.Id == id);
            if (log == null)
            {
                return NotFound();
            }

            return Json(log);
        }
    }
}
