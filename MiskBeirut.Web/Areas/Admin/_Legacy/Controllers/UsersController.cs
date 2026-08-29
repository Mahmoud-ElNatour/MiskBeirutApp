using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    public class UsersController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole<int>> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            UserManager<User> userManager,
            RoleManager<IdentityRole<int>> roleManager,
            ApplicationDbContext context,
            ILogger<UsersController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _logger = logger;
        }

        // GET: /control-panel/users
        [HttpGet("/control-panel/users")]
        public async Task<IActionResult> Index([FromQuery] string? role)
        {
            _logger.LogInformation("Retrieving users list with role filter: {Role}", role ?? "All");

            var users = _userManager.Users.AsQueryable();

            if (!string.IsNullOrEmpty(role))
            {
                users = users.Where(u => u.Role == role);
            }

            var userList = await users
                .Select(u => new
                {
                    u.Id,
                    u.UserName,
                    u.Email,
                    u.Role,
                    u.CreatedAt,
                    HasCustomerProfile = u.CustomerProfile != null,
                    HasEmployeeProfile = u.EmployeeProfile != null,
                    CustomerName = u.CustomerProfile != null ? u.CustomerProfile.Username : null,
                    EmployeeName = u.EmployeeProfile != null ? u.EmployeeProfile.Name : null
                })
                .ToListAsync();

            ViewData["SelectedRole"] = role ?? "";
            ViewData["Roles"] = new[] { "admin", "user", "manager" };

            return View(userList);
        }

        // GET: /api/users
        [HttpGet("/api/users")]
        public async Task<IActionResult> GetUsers([FromQuery] string? role)
        {
            var users = _userManager.Users.AsQueryable();

            if (!string.IsNullOrEmpty(role))
            {
                users = users.Where(u => u.Role == role);
            }

            var userList = await users
                .Include(u => u.CustomerProfile)
                .Include(u => u.EmployeeProfile)
                .ToListAsync();

            return Json(new { status = "success", data = userList });
        }

        // GET: /api/users/{id}/assign
        [HttpGet("/api/users/{id:int}/assign")]
        public async Task<IActionResult> GetAssignmentOptions(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                return NotFound(new { error = "User not found" });
            }

            var availableCustomers = await _context.Customers
                .Where(c => c.UserId == null || c.UserId == id)
                .ToListAsync();

            var availableEmployees = await _context.Employees
                .Where(e => e.UserId == null || e.UserId == id)
                .ToListAsync();

            var currentCustomer = await _context.Customers
                .FirstOrDefaultAsync(c => c.UserId == id);

            var currentEmployee = await _context.Employees
                .FirstOrDefaultAsync(e => e.UserId == id);

            return Json(new
            {
                status = "success",
                user = new { user.Id, user.UserName, user.Email, user.Role },
                availableCustomers = availableCustomers.Select(c => new { c.Id, c.Username }),
                availableEmployees = availableEmployees.Select(e => new { e.Id, e.Name }),
                currentCustomer = currentCustomer != null ? new { currentCustomer.Id, currentCustomer.Username } : null,
                currentEmployee = currentEmployee != null ? new { currentEmployee.Id, currentEmployee.Name } : null
            });
        }

        // PUT: /api/users/{id}/assign-customer
        [HttpPut("/api/users/{id:int}/assign-customer")]
        public async Task<IActionResult> AssignCustomer(int id, [FromBody] AssignmentRequest request)
        {
            try
            {
                _logger.LogInformation("Assigning customer {CustomerId} to user {UserId}", request.CustomerId, id);

                var user = await _userManager.FindByIdAsync(id.ToString());
                if (user == null)
                {
                    return NotFound(new { error = "User not found" });
                }

                // Remove existing customer assignment
                var existingCustomer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == id);
                if (existingCustomer != null)
                {
                    existingCustomer.UserId = null;
                }

                // Assign new customer
                if (request.CustomerId.HasValue)
                {
                    var customer = await _context.Customers.FindAsync(request.CustomerId.Value);
                    if (customer == null)
                    {
                        return NotFound(new { error = "Customer not found" });
                    }

                    // Remove from other users if assigned
                    var otherAssignment = await _context.Customers
                        .FirstOrDefaultAsync(c => c.Id == request.CustomerId.Value && c.UserId != null && c.UserId != id);
                    if (otherAssignment != null)
                    {
                        otherAssignment.UserId = null;
                    }

                    customer.UserId = id;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully assigned customer {CustomerId} to user {UserId}", request.CustomerId, id);

                return Json(new { status = "success", message = "Customer assignment updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to assign customer to user {UserId}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // PUT: /api/users/{id}/assign-employee
        [HttpPut("/api/users/{id:int}/assign-employee")]
        public async Task<IActionResult> AssignEmployee(int id, [FromBody] AssignmentRequest request)
        {
            try
            {
                _logger.LogInformation("Assigning employee {EmployeeId} to user {UserId}", request.EmployeeId, id);

                var user = await _userManager.FindByIdAsync(id.ToString());
                if (user == null)
                {
                    return NotFound(new { error = "User not found" });
                }

                // Remove existing employee assignment
                var existingEmployee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == id);
                if (existingEmployee != null)
                {
                    existingEmployee.UserId = null;
                }

                // Assign new employee
                if (request.EmployeeId.HasValue)
                {
                    var employee = await _context.Employees.FindAsync(request.EmployeeId.Value);
                    if (employee == null)
                    {
                        return NotFound(new { error = "Employee not found" });
                    }

                    // Remove from other users if assigned
                    var otherAssignment = await _context.Employees
                        .FirstOrDefaultAsync(e => e.Id == request.EmployeeId.Value && e.UserId != null && e.UserId != id);
                    if (otherAssignment != null)
                    {
                        otherAssignment.UserId = null;
                    }

                    employee.UserId = id;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully assigned employee {EmployeeId} to user {UserId}", request.EmployeeId, id);

                return Json(new { status = "success", message = "Employee assignment updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to assign employee to user {UserId}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // PUT: /api/users/{id}/change-role
        [HttpPut("/api/users/{id:int}/change-role")]
        public async Task<IActionResult> ChangeRole(int id, [FromBody] ChangeRoleRequest request)
        {
            try
            {
                _logger.LogInformation("Changing role for user {UserId} to {Role}", id, request.Role);

                var user = await _userManager.FindByIdAsync(id.ToString());
                if (user == null)
                {
                    return NotFound(new { error = "User not found" });
                }

                user.Role = request.Role;
                await _userManager.UpdateAsync(user);

                _logger.LogInformation("Successfully changed role for user {UserId} to {Role}", id, request.Role);

                return Json(new { status = "success", message = "Role updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to change role for user {UserId}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    public class AssignmentRequest
    {
        public int? CustomerId { get; set; }
        public int? EmployeeId { get; set; }
    }

    public class ChangeRoleRequest
    {
        public string Role { get; set; } = null!;
    }
}
