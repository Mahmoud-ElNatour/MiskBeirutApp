using Microsoft.AspNetCore.Mvc;
using MiskBeirut.Application.DTOs;
using MiskBeirut.Application.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MiskBeirut.Web.Controllers
{
    [Route("[controller]")]
    [Route("api/[controller]")]
    public class LogsController : Controller
    {
        private readonly ILogService _logService;
        private readonly ILogger<LogsController> _logger;

        public LogsController(ILogService logService, ILogger<LogsController> logger)
        {
            _logService = logService;
            _logger = logger;
        }

        // GET: /Logs
        [HttpGet]
        public async Task<IActionResult> Index(
            [FromQuery] string? level = null,
            [FromQuery] string? username = null,
            [FromQuery] string? action = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 50;

            _logger.LogInformation("Querying Logs with level: {Level}, username: {Username}, action: {Action}, page: {Page}, pageSize: {PageSize}", level, username, action, page, pageSize);
            var (logs, totalItems) = await _logService.GetLogsAsync(level, username, action, page, pageSize);

            ViewData["Level"] = level;
            ViewData["Username"] = username;
            ViewData["Action"] = action;
            ViewData["Page"] = page;
            ViewData["PageSize"] = pageSize;
            ViewData["TotalItems"] = totalItems;

            if (Request.Path.Value?.Contains("/api/") == true)
            {
                Response.Headers.Append("X-Total-Count", totalItems.ToString());
                Response.Headers.Append("X-Page-Number", page.ToString());
                Response.Headers.Append("X-Page-Size", pageSize.ToString());
                return Json(logs);
            }

            return View(logs);
        }

        // GET: /Logs/5 or /api/Logs/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetLog(int id)
        {
            _logger.LogInformation("Retrieving Logs with ID {ResourceId}", id);
            var log = await _logService.GetLogAsync(id);
            if (log == null)
            {
                _logger.LogWarning("Logs with ID {ResourceId} was not found.", id);
                return NotFound();
            }
            return Json(log);
        }
    }
}
