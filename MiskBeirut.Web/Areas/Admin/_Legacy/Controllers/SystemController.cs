using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiskBeirut.Application.DTOs;
using MiskBeirut.Application.Services;
using MiskBeirut.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using MiskBeirut.Core.Entities;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace MiskBeirut.Web.Controllers
{
    [Route("[controller]")]
    [Route("api/[controller]")]
    [Authorize(Roles = "admin")]
    public class SystemController : Controller
    {
        private readonly ISystemService _systemService;
        private readonly IWebHostEnvironment _env;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole<int>> _roleManager;
        private readonly ILogger<SystemController> _logger;

        public SystemController(ISystemService systemService, IWebHostEnvironment env, ApplicationDbContext context, UserManager<User> userManager, RoleManager<IdentityRole<int>> roleManager, ILogger<SystemController> logger)
        {
            _systemService = systemService;
            _env = env;
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        // GET: /System/ControlPanel
        [HttpGet("ControlPanel")]
        public IActionResult ControlPanel()
        {
            ViewData["Title"] = "Control Panel";
            return View();
        }

        // GET: /control-panel/reports
        [HttpGet("/control-panel/reports")]
        public async Task<IActionResult> ReportsPage([FromQuery] int? month, [FromQuery] int? year)
        {
            try
            {
                var now = DateTime.UtcNow;
                int selectedMonth = month ?? now.Month;
                int selectedYear = year ?? now.Year;

                var closings = await _context.DailyClosings
                    .Where(c => c.Date.Year == selectedYear && c.Date.Month == selectedMonth)
                    .OrderBy(c => c.Date)
                    .ToListAsync();

                var customers = await _context.Customers.ToListAsync();
                var employees = await _context.Employees.Where(e => e.IsActive).ToListAsync();
                var expenses = await _context.Expenses.Where(e => e.Date.Year == selectedYear && e.Date.Month == selectedMonth).ToListAsync();

                var reportData = (
                    TotalDailyClosings: closings.Count,
                    TotalExpenses: expenses.Sum(e => (int)e.Amount),
                    TotalCustomers: customers.Count,
                    TotalEmployees: employees.Count,
                    Customers: customers.AsEnumerable()
                );

                ViewData["CurrentMonth"] = selectedMonth;
                ViewData["CurrentYear"] = selectedYear;
                return View("Reports", reportData);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading reports data: " + ex.Message;
                return RedirectToAction("ControlPanel", "System");
            }
        }

        #region Site Settings

        // GET: /System/settings or /control-panel/site-settings
        [HttpGet("/control-panel/site-settings")]
        [HttpGet("settings")]
        public async Task<IActionResult> GetSettings()
        {
            var settingsList = await _systemService.GetSettingsAsync();
            var settings = settingsList.ToDictionary(s => s.Key, s => s.Value ?? "");
            return View("SiteSettings", settings);
        }

        // GET: /System/settings/title
        [HttpGet("settings/{key}")]
        public async Task<IActionResult> GetSetting(string key)
        {
            var setting = await _systemService.GetSettingAsync(key);
            if (setting == null) return NotFound();
            return Json(setting);
        }

        // POST: /api/site-settings or /System/settings
        [HttpPost("/api/site-settings")]
        [HttpPost("settings")]
        public async Task<IActionResult> UpdateSiteSettings()
        {
            _logger.LogInformation("Initiating site settings update.");
            var keys = new[] { "brand_name", "address", "hours", "phone_display", "phone_tel", "instagram_url", "maps_url", "menu_url" };
            var updatedById = int.TryParse(_userManager.GetUserId(User), out var id) ? (int?)id : null;

            if (Request.HasFormContentType)
            {
                foreach (var key in keys)
                {
                    if (Request.Form.TryGetValue(key, out var val))
                    {
                        _logger.LogInformation("Updating site setting key: {SettingKey}", key);
                        await _systemService.UpsertSettingAsync(new SiteSettingsDto(0, key, val.ToString(), DateTime.UtcNow, updatedById));
                    }
                }
                _logger.LogInformation("Successfully updated site settings (Form POST).");
                TempData["Success"] = "Site settings updated successfully!";
                return Redirect("/control-panel/site-settings");
            }
            else
            {
                var data = await Request.ReadFromJsonAsync<Dictionary<string, string>>();
                if (data != null)
                {
                    foreach (var key in keys)
                    {
                        if (data.TryGetValue(key, out var val))
                        {
                            _logger.LogInformation("Updating site setting key: {SettingKey}", key);
                            await _systemService.UpsertSettingAsync(new SiteSettingsDto(0, key, val, DateTime.UtcNow, updatedById));
                        }
                    }
                }
                _logger.LogInformation("Successfully updated site settings (JSON API).");
                return Json(new { success = true, toast = new { type = "success", title = "Updated", message = "Site settings updated successfully!" } });
            }
        }

        #endregion

        #region Gallery Images

        // GET: /control-panel/site-gallery or /System/gallery
        [HttpGet("/control-panel/site-gallery")]
        [HttpGet("gallery")]
        public async Task<IActionResult> SiteGallery()
        {
            var images = await _systemService.GetGalleryAsync();
            if (Request.Path.Value?.Contains("/api/") == true || Request.Headers["Accept"].ToString().Contains("application/json"))
            {
                return Json(images);
            }
            return View("SiteGallery", images);
        }

        // POST: /api/site-gallery/add
        [HttpPost("/api/site-gallery/add")]
        public async Task<IActionResult> AddGalleryImage([FromForm] IFormFile image, [FromForm] string? alt_text)
        {
            if (image == null || image.Length == 0)
            {
                _logger.LogWarning("AddGalleryImage failed: No file uploaded.");
                TempData["Error"] = "No image selected.";
                return Redirect("/control-panel/site-gallery");
            }

            try
            {
                _logger.LogInformation("Uploading new gallery image: {FileName}", image.FileName);
                var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var uploadsFolder = Path.Combine(webRoot, "img", "gallery");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = $"{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}_{Path.GetFileName(image.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(fileStream);
                }

                var relativePath = "/img/gallery/" + uniqueFileName;
                
                var gallery = await _systemService.GetGalleryAsync();
                var maxOrder = gallery.Any() ? gallery.Max(g => g.SortOrder) : 0;

                await _systemService.AddGalleryImageAsync(new SiteGalleryImagesDto(0, relativePath, alt_text, maxOrder + 1, true));

                _logger.LogInformation("Successfully added gallery image path: {RelativePath}", relativePath);
                TempData["Success"] = "Image uploaded to gallery successfully.";
                return Redirect("/control-panel/site-gallery");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload gallery image");
                TempData["Error"] = $"Error uploading image: {ex.Message}";
                return Redirect("/control-panel/site-gallery");
            }
        }

        // PUT: /api/site-gallery/5
        [HttpPut("/api/site-gallery/{id:int}")]
        [HttpPut("gallery/{id:int}")]
        public async Task<IActionResult> UpdateGalleryImage(int id, [FromBody] GalleryImageUpdateRequest input)
        {
            _logger.LogInformation("Updating gallery image ID {ResourceId}", id);
            var existing = (await _systemService.GetGalleryAsync()).FirstOrDefault(g => g.Id == id);
            if (existing == null)
            {
                _logger.LogWarning("Gallery image ID {ResourceId} not found for update.", id);
                return NotFound(new { status = "error", message = "Image not found." });
            }

            var updatedDto = new SiteGalleryImagesDto(
                existing.Id,
                existing.ImageUrl,
                existing.AltText,
                input.sort_order ?? existing.SortOrder,
                input.is_active ?? existing.IsActive
            );

            try
            {
                var success = await _systemService.UpdateGalleryImageAsync(id, updatedDto);
                if (!success)
                {
                    _logger.LogWarning("Gallery image ID {ResourceId} update failed.", id);
                    return NotFound(new { status = "error", message = "Update failed." });
                }
                _logger.LogInformation("Successfully updated gallery image ID {ResourceId}", id);
                return Json(new { status = "success", message = "Gallery image updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update gallery image ID {ResourceId}", id);
                return Json(new { status = "error", message = ex.Message });
            }
        }

        public class GalleryImageUpdateRequest
        {
            public int? sort_order { get; set; }
            public bool? is_active { get; set; }
        }

        // DELETE: /api/site-gallery/5
        [HttpDelete("/api/site-gallery/{id:int}")]
        [HttpDelete("gallery/{id:int}")]
        public async Task<IActionResult> DeleteGalleryImage(int id)
        {
            try
            {
                _logger.LogWarning("Initiating deletion of gallery image ID {ResourceId}", id);
                var galleryItems = await _systemService.GetGalleryAsync();
                var item = galleryItems.FirstOrDefault(g => g.Id == id);
                if (item != null)
                {
                    var relativePath = item.ImageUrl.TrimStart('/');
                    var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    var physicalPath = Path.Combine(webRoot, relativePath);
                    if (System.IO.File.Exists(physicalPath))
                    {
                        _logger.LogInformation("Deleting physical image file: {PhysicalPath}", physicalPath);
                        System.IO.File.Delete(physicalPath);
                    }
                }

                var success = await _systemService.DeleteGalleryImageAsync(id);
                if (!success)
                {
                    _logger.LogWarning("Gallery image ID {ResourceId} was not found for deletion.", id);
                    return NotFound();
                }
                _logger.LogInformation("Successfully deleted gallery image ID {ResourceId}", id);
                return Json(new { success = true, status = "success", message = "Gallery image deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete gallery image ID {ResourceId}", id);
                return Json(new { status = "error", message = ex.Message });
            }
        }

        #endregion

        #region User Management & Reports & Exports

        // GET: /control-panel/users
        [HttpGet("/control-panel/users-old")]
        public async Task<IActionResult> UsersPage()
        {
            try
            {
                var users = await _userManager.Users.OrderBy(u => u.UserName).ToListAsync();
                var roles = await _roleManager.Roles.OrderBy(r => r.Name).Select(r => r.Name).ToListAsync();
                ViewBag.Roles = roles;
                return View("Users", users);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading users: " + ex.Message;
                return RedirectToAction("ControlPanel", "System");
            }
        }

        // GET: /control-panel/roles
        [HttpGet("/control-panel/roles")]
        public async Task<IActionResult> RolesPage()
        {
            try
            {
                var roles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
                var roleData = new List<(IdentityRole<int> Role, int UserCount)>();
                foreach (var role in roles)
                {
                    var users = await _userManager.GetUsersInRoleAsync(role.Name!);
                    roleData.Add((role, users.Count));
                }
                return View("Roles", roleData);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading roles: " + ex.Message;
                return RedirectToAction("ControlPanel", "System");
            }
        }

        // GET: /api/roles
        [HttpGet("/api/roles")]
        public async Task<IActionResult> GetRoles()
        {
            var roles = await _roleManager.Roles.OrderBy(r => r.Name).Select(r => new { r.Id, r.Name }).ToListAsync();
            return Json(roles);
        }

        public record CreateRoleRequest(string Name);

        // POST: /api/roles
        [HttpPost("/api/roles")]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return Json(new { status = "error", message = "Role name is required." });

            if (await _roleManager.RoleExistsAsync(request.Name))
                return Json(new { status = "error", message = "Role already exists." });

            _logger.LogInformation("Creating new role: {RoleName}", request.Name);
            var result = await _roleManager.CreateAsync(new IdentityRole<int>(request.Name.Trim().ToLower()));
            if (result.Succeeded)
                return Json(new { status = "success", message = "Role created successfully." });

            return Json(new { status = "error", message = string.Join(" ", result.Errors.Select(e => e.Description)) });
        }

        // DELETE: /api/roles/{id}
        [HttpDelete("/api/roles/{id:int}")]
        public async Task<IActionResult> DeleteRole(int id)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null) return NotFound(new { status = "error", message = "Role not found." });

            if (role.Name?.ToLower() == "admin")
                return Json(new { status = "error", message = "Cannot delete the admin role." });

            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
            if (usersInRole.Count > 0)
                return StatusCode(409, new
                {
                    status = "error",
                    toast = new { type = "error", title = "Delete blocked", message = $"Role is assigned to {usersInRole.Count} user(s). Reassign them first." }
                });

            _logger.LogWarning("Deleting role: {RoleName}", role.Name);
            var result = await _roleManager.DeleteAsync(role);
            if (result.Succeeded)
                return Json(new { status = "success", message = "Role deleted successfully." });

            return Json(new { status = "error", message = string.Join(" ", result.Errors.Select(e => e.Description)) });
        }

        // GET: /api/users/5
        [HttpGet("/api/users/{id:int}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) return NotFound();
            return Json(new
            {
                id = user.Id,
                username = user.UserName,
                email = user.Email,
                role = user.Role
            });
        }

        public record CreateUserRequest(string Username, string Email, string Role, string Password);

        // POST: /api/users
        [HttpPost("/api/users")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                _logger.LogWarning("CreateUser failed: Missing username or password.");
                return Json(new { status = "error", message = "Username and password are required." });
            }

            _logger.LogInformation("Creating new User: {Username} with role: {Role}", request.Username, request.Role);
            var existing = await _userManager.FindByNameAsync(request.Username);
            if (existing != null)
            {
                _logger.LogWarning("CreateUser failed: Username {Username} already exists.", request.Username);
                return Json(new { status = "error", message = "Username already exists." });
            }

            var user = new User
            {
                UserName = request.Username,
                Email = request.Email,
                Role = request.Role ?? "user",
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (result.Succeeded)
            {
                var roleName = request.Role ?? "user";
                if (await _roleManager.RoleExistsAsync(roleName))
                    await _userManager.AddToRoleAsync(user, roleName);

                _logger.LogInformation("Successfully created User ID: {ResourceId} ({Username})", user.Id, request.Username);
                return Json(new { status = "success", message = "User created successfully", id = user.Id });
            }

            _logger.LogWarning("CreateUser failed with errors: {Errors}", string.Join(" ", result.Errors.Select(e => e.Description)));
            return Json(new { status = "error", message = string.Join(" ", result.Errors.Select(e => e.Description)) });
        }

        public record UpdateUserRequest(string Username, string Email, string Role, string? Password);

        // PUT: /api/users/{id:int}
        [HttpPut("/api/users/{id:int}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
        {
            _logger.LogInformation("Updating User ID {ResourceId}", id);
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                _logger.LogWarning("UpdateUser failed: User ID {ResourceId} was not found.", id);
                return NotFound();
            }

            user.UserName = request.Username;
            user.Email = request.Email;
            if (!string.IsNullOrEmpty(request.Role))
            {
                user.Role = request.Role;
                var currentRoles = await _userManager.GetRolesAsync(user);
                if (currentRoles.Count > 0)
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (await _roleManager.RoleExistsAsync(request.Role))
                    await _userManager.AddToRoleAsync(user, request.Role);
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                _logger.LogWarning("UpdateUser failed: Errors updating User ID {ResourceId}: {Errors}", id, string.Join(" ", result.Errors.Select(e => e.Description)));
                return Json(new { status = "error", message = string.Join(" ", result.Errors.Select(e => e.Description)) });
            }

            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                _logger.LogInformation("Resetting password for User ID {ResourceId}", id);
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passResult = await _userManager.ResetPasswordAsync(user, token, request.Password);
                if (!passResult.Succeeded)
                {
                    _logger.LogWarning("UpdateUser password reset failed for User ID {ResourceId}: {Errors}", id, string.Join(" ", passResult.Errors.Select(e => e.Description)));
                    return Json(new { status = "error", message = "User updated but password reset failed: " + string.Join(" ", passResult.Errors.Select(e => e.Description)) });
                }
            }

            _logger.LogInformation("Successfully updated User ID {ResourceId}", id);
            return Json(new { status = "success", message = "User updated successfully" });
        }

        // DELETE: /api/users/5
        [HttpDelete("/api/users/{id:int}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                _logger.LogWarning("Initiating deletion of User ID {ResourceId}", id);
                var user = await _userManager.FindByIdAsync(id.ToString());
                if (user == null)
                {
                    _logger.LogWarning("DeleteUser failed: User ID {ResourceId} was not found.", id);
                    return NotFound();
                }

                if (user.UserName?.ToLower() == "admin")
                {
                    _logger.LogWarning("DeleteUser failed: Attempted to delete protected 'admin' user.");
                    return Json(new { status = "error", message = "Cannot delete admin user" });
                }

                // Dependency check
                var logsCount = await _context.Logs.CountAsync(l => l.UserId == id);
                if (logsCount > 0)
                {
                    var conflictMsg = $"Cannot delete this user because it is used in {logsCount} records.";
                    _logger.LogWarning("DeleteUser failed: User ID {ResourceId} has active dependencies ({DependencyCount} logs).", id, logsCount);
                    return StatusCode(409, new
                    {
                        success = false,
                        error = new
                        {
                            code = "CONFLICT",
                            message = conflictMsg,
                            details = new
                            {
                                dependencies = new[] { new { entity = "logs", count = logsCount } }
                            }
                        },
                        toast = new
                        {
                            type = "error",
                            title = "Delete blocked",
                            message = "Cannot delete because it is used in other records."
                        },
                        requestId = HttpContext.TraceIdentifier
                    });
                }

                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Successfully deleted User ID {ResourceId}", id);
                    return Json(new
                    {
                        status = "success",
                        ok = true,
                        message = "User deleted successfully",
                        toast = new
                        {
                            type = "success",
                            title = "Success",
                            message = "User deleted successfully."
                        }
                    });
                }

                _logger.LogWarning("DeleteUser failed for User ID {ResourceId} with errors: {Errors}", id, string.Join(" ", result.Errors.Select(e => e.Description)));
                return Json(new { status = "error", message = string.Join(" ", result.Errors.Select(e => e.Description)) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete User ID {ResourceId}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        public record ReportRequestDto(int Month, int Year, int? CustomerId);

        // POST: /api/reports/sales
        [HttpPost("/api/reports/sales")]
        public async Task<IActionResult> SalesReport([FromBody] ReportRequestDto request)
        {
            try
            {
                var closings = await _context.DailyClosings
                    .Where(c => c.Date.Year == request.Year && c.Date.Month == request.Month)
                    .ToListAsync();

                decimal totalSales = closings.Sum(c => c.MainReading);
                decimal totalCustomerBalance = await _context.Customers.SumAsync(c => c.Balance);
                decimal actualCash = totalSales + totalCustomerBalance;

                var reportData = new
                {
                    month = request.Month,
                    year = request.Year,
                    total_sales = totalSales,
                    actual_cash = actualCash,
                    total_customer_balance = totalCustomerBalance,
                    sales = closings.Select(c => new
                    {
                        date = c.Date.ToString("yyyy-MM-dd"),
                        amount = c.AdjustedReading,
                        main_reading = c.MainReading,
                        dr_smashed = c.DrSmashed
                    }).ToList()
                };

                return Json(new
                {
                    status = "success",
                    data = reportData
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to generate sales report", details = ex.Message });
            }
        }

        // POST: /api/reports/payroll
        [HttpPost("/api/reports/payroll")]
        public async Task<IActionResult> PayrollReport([FromBody] ReportRequestDto request)
        {
            try
            {
                var workings = await _context.EmployeeWorkings
                    .Include(w => w.Employee)
                    .Where(w => w.Year == request.Year && w.Month == request.Month)
                    .ToListAsync();

                decimal totalPayroll = workings.Sum(w => w.Total);
                decimal totalDeductions = workings.Sum(w => w.DeductionsTotal);

                var reportData = new
                {
                    month = request.Month,
                    year = request.Year,
                    total_payroll = totalPayroll,
                    total_employees = workings.Count,
                    total_deductions = totalDeductions,
                    employees = workings.Select(w => new
                    {
                        name = w.Employee.Name,
                        position = w.Employee.Position,
                        base_salary = w.Employee.BaseSalary,
                        advance = w.AdvanceTotal,
                        deductions = w.DeductionsTotal,
                        actual_salary = w.ActualSalary,
                        total = w.Total
                    }).ToList()
                };

                return Json(new
                {
                    status = "success",
                    data = reportData
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to generate payroll report", details = ex.Message });
            }
        }

        // POST: /api/reports/expenses
        [HttpPost("/api/reports/expenses")]
        public async Task<IActionResult> ExpensesReport([FromBody] ReportRequestDto request)
        {
            try
            {
                var generalExpenses = await _context.Expenses
                    .Include(e => e.Receiver)
                    .Where(e => e.Date.Year == request.Year && e.Date.Month == request.Month)
                    .ToListAsync();

                var samerExpenses = await _context.SamerExpenses
                    .Include(e => e.Receiver)
                    .Where(e => e.Date.Year == request.Year && e.Date.Month == request.Month)
                    .ToListAsync();

                decimal totalGenExpenses = generalExpenses.Sum(e => e.Amount);
                decimal totalSamerExpenses = samerExpenses.Sum(e => e.Amount);
                decimal totalExpenses = totalGenExpenses + totalSamerExpenses;

                var receiverBreakdown = new Dictionary<string, double>();
                var samerReceiverBreakdown = new Dictionary<string, double>();
                var uniqueReceivers = new HashSet<int>();

                var allExpensesList = new List<object>();

                foreach (var exp in generalExpenses)
                {
                    string name = exp.Receiver?.Name ?? "Unassigned";
                    if (exp.ReceiverId.HasValue)
                    {
                        uniqueReceivers.Add(exp.ReceiverId.Value);
                    }

                    receiverBreakdown[name] = receiverBreakdown.GetValueOrDefault(name, 0) + (double)exp.Amount;
                    allExpensesList.Add(new
                    {
                        type = "General",
                        date = exp.Date.ToString("yyyy-MM-dd"),
                        receiver = name,
                        amount = (double)exp.Amount,
                        note = exp.Note ?? ""
                    });
                }

                foreach (var exp in samerExpenses)
                {
                    string name = exp.Receiver?.Name ?? "Unassigned";
                    samerReceiverBreakdown[name] = samerReceiverBreakdown.GetValueOrDefault(name, 0) + (double)exp.Amount;
                    allExpensesList.Add(new
                    {
                        type = "Samer",
                        date = exp.Date.ToString("yyyy-MM-dd"),
                        receiver = name,
                        amount = (double)exp.Amount,
                        note = exp.Note ?? ""
                    });
                }

                var reportData = new
                {
                    month = request.Month,
                    year = request.Year,
                    total_expenses = totalExpenses,
                    total_receivers = uniqueReceivers.Count,
                    receiver_breakdown = receiverBreakdown,
                    samer_receiver_breakdown = samerReceiverBreakdown,
                    expenses = allExpensesList
                };

                return Json(new
                {
                    status = "success",
                    data = reportData
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private string GenerateCsv(IEnumerable<string> headers, IEnumerable<IEnumerable<string>> rows)
        {
            var builder = new StringBuilder();
            builder.AppendLine(string.Join(",", headers.Select(EscapeCsv)));
            foreach (var row in rows)
            {
                builder.AppendLine(string.Join(",", row.Select(EscapeCsv)));
            }
            return builder.ToString();
        }

        private string EscapeCsv(string? val)
        {
            if (val == null) return "";
            if (val.Contains(",") || val.Contains("\"") || val.Contains("\n") || val.Contains("\r"))
            {
                return $"\"{val.Replace("\"", "\"\"")}\"";
            }
            return val;
        }

        // GET: /api/exports/sales
        [HttpGet("/api/exports/sales")]
        public async Task<IActionResult> ExportSalesCsv([FromQuery] int? month, [FromQuery] int? year)
        {
            var now = DateTime.UtcNow;
            int m = month ?? now.Month;
            int y = year ?? now.Year;

            var closings = await _context.DailyClosings
                .Where(c => c.Date.Year == y && c.Date.Month == m)
                .OrderBy(c => c.Date)
                .ToListAsync();

            var headers = new[] { "Date", "Main Reading", "Expenses", "Advances", "Credits", "Cashback", "Deductions", "5% Fee", "Actual Cash" };
            var rows = new List<List<string>>();

            decimal totalMainReading = 0;

            foreach (var c in closings)
            {
                totalMainReading += c.MainReading;
                rows.Add(new List<string>
                {
                    c.Date.ToString("yyyy-MM-dd"),
                    c.MainReading.ToString("F2"),
                    c.TotalExpenses.ToString("F2"),
                    c.TotalAdvance.ToString("F2"),
                    c.TotalCredit.ToString("F2"),
                    c.TotalCashback.ToString("F2"),
                    c.TotalDeductions.ToString("F2"),
                    c.FivePercent.ToString("F2"),
                    c.ActualCash.ToString("F2")
                });
            }

            decimal totalCustomersBalance = await _context.Customers.SumAsync(cust => cust.Balance);
            decimal finalActualCash = totalMainReading + totalCustomersBalance;

            // Summary rows
            rows.Add(new List<string>());
            rows.Add(new List<string> { "--- SUMMARY ---", "", "", "", "", "", "", "", "" });
            rows.Add(new List<string> { "Total Main Readings", totalMainReading.ToString("F2"), "", "", "", "", "", "", "" });
            rows.Add(new List<string> { "Total Customer Balances", totalCustomersBalance.ToString("F2"), "", "", "", "", "", "", "" });
            rows.Add(new List<string> { "Final Actual Cash", finalActualCash.ToString("F2"), "", "", "", "", "", "", "" });

            var csvContent = GenerateCsv(headers, rows);
            var bytes = Encoding.UTF8.GetBytes(csvContent);
            return File(bytes, "text/csv", $"sales_export_{y}_{m}.csv");
        }

        // GET: /api/exports/payroll
        [HttpGet("/api/exports/payroll")]
        public async Task<IActionResult> ExportPayrollCsv([FromQuery] int? month, [FromQuery] int? year)
        {
            var now = DateTime.UtcNow;
            int m = month ?? now.Month;
            int y = year ?? now.Year;

            var workings = await _context.EmployeeWorkings
                .Include(w => w.Employee)
                .Where(w => w.Year == y && w.Month == m && w.IsWorking)
                .ToListAsync();

            foreach (var record in workings)
            {
                record.CalculateSalary();
            }

            var headers = new[] { "Employee", "Position", "Base Salary", "Working Days", "Actual Working Days", "Advance", "Deductions", "Actual Salary", "Total" };
            var rows = new List<List<string>>();

            decimal sumBase = 0;
            decimal sumAdvance = 0;
            decimal sumDeductions = 0;
            decimal sumActualSalary = 0;
            decimal sumTotal = 0;

            foreach (var w in workings)
            {
                decimal baseSal = w.Employee.BaseSalary;
                decimal adv = w.AdvanceTotal;
                decimal ded = w.DeductionsTotal;
                decimal act = w.ActualSalary;
                decimal tot = w.Total;

                sumBase += baseSal;
                sumAdvance += adv;
                sumDeductions += ded;
                sumActualSalary += act;
                sumTotal += tot;

                rows.Add(new List<string>
                {
                    w.Employee.Name,
                    w.Employee.Position ?? "N/A",
                    baseSal.ToString("F2"),
                    w.WorkingDays.ToString("F1"),
                    w.ActualWorkingDays.ToString("F1"),
                    adv.ToString("F2"),
                    ded.ToString("F2"),
                    act.ToString("F2"),
                    tot.ToString("F2")
                });
            }

            rows.Add(new List<string>());
            rows.Add(new List<string>
            {
                "TOTAL",
                "",
                sumBase.ToString("F2"),
                "",
                "",
                sumAdvance.ToString("F2"),
                sumDeductions.ToString("F2"),
                sumActualSalary.ToString("F2"),
                sumTotal.ToString("F2")
            });

            var csvContent = GenerateCsv(headers, rows);
            var bytes = Encoding.UTF8.GetBytes(csvContent);
            return File(bytes, "text/csv", $"payroll_export_{y}_{m}.csv");
        }

        // GET: /api/exports/reports
        [HttpGet("/api/exports/reports")]
        public async Task<IActionResult> ExportReportsCsv([FromQuery] int? month, [FromQuery] int? year)
        {
            var now = DateTime.UtcNow;
            int m = month ?? now.Month;
            int y = year ?? now.Year;

            var generalResults = await _context.Receivers
                .Select(r => new
                {
                    Receiver = r,
                    PeriodTotal = _context.Expenses.Where(e => e.ReceiverId == r.Id && e.Date.Year == y && e.Date.Month == m).Sum(e => (decimal?)e.Amount) ?? 0
                })
                .OrderBy(r => r.Receiver.Name)
                .ToListAsync();

            var samerResults = await _context.SamerExpenseReceivers
                .Select(r => new
                {
                    Receiver = r,
                    PeriodTotal = _context.SamerExpenses.Where(e => e.ReceiverId == r.Id && e.Date.Year == y && e.Date.Month == m).Sum(e => (decimal?)e.Amount) ?? 0
                })
                .OrderBy(r => r.Receiver.Name)
                .ToListAsync();

            decimal totalGeneral = generalResults.Sum(r => r.PeriodTotal);
            decimal totalSamer = samerResults.Sum(r => r.PeriodTotal);

            var headers = new[] { "Type", "Name", $"Total Amount ({m}/{y})", "All-Time Total" };
            var rows = new List<List<string>>();

            rows.Add(new List<string> { "--- GENERAL EXPENSES ---", "", "", "" });
            foreach (var r in generalResults)
            {
                if (r.PeriodTotal > 0)
                {
                    rows.Add(new List<string>
                    {
                        "General",
                        r.Receiver.Name,
                        r.PeriodTotal.ToString("F2"),
                        r.Receiver.PaidAmount.ToString("F2")
                    });
                }
            }

            rows.Add(new List<string>());
            rows.Add(new List<string> { "--- SAMER EXPENSES ---", "", "", "" });
            foreach (var r in samerResults)
            {
                if (r.PeriodTotal > 0)
                {
                    rows.Add(new List<string>
                    {
                        "Samer",
                        r.Receiver.Name,
                        r.PeriodTotal.ToString("F2"),
                        r.Receiver.PaidAmount.ToString("F2")
                    });
                }
            }

            rows.Add(new List<string>());
            rows.Add(new List<string> { "SUMMARY TOTALS", "", "", "" });
            rows.Add(new List<string> { "General Expenses Total", totalGeneral.ToString("F2"), "", "" });
            rows.Add(new List<string> { "Samer Expenses Total", totalSamer.ToString("F2"), "", "" });
            rows.Add(new List<string> { "GRAND TOTAL", (totalGeneral + totalSamer).ToString("F2"), "", "" });

            var csvContent = GenerateCsv(headers, rows);
            var bytes = Encoding.UTF8.GetBytes(csvContent);
            return File(bytes, "text/csv", $"expenses_export_{y}_{m}.csv");
        }

        #endregion
    }
}
