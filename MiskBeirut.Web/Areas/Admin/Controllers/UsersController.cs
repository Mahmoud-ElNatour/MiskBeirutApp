using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiskBeirut.Application.Managers;
using MiskBeirut.Core.Constants;
using MiskBeirut.Core.Entities;
using MiskBeirut.Web.Areas.Admin.Models.Users;
using MiskBeirut.Web.Authorization;

namespace MiskBeirut.Web.Areas.Admin.Controllers;

[RequirePrivilege("Users")]
public class UsersController : AdminControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole<int>> _roleManager;
    private readonly EmployeeManager _employees;
    private readonly AuditLogManager _auditLogs;

    public UsersController(UserManager<User> userManager, RoleManager<IdentityRole<int>> roleManager, EmployeeManager employees, AuditLogManager auditLogs, BackofficePageContentManager pages) : base(pages)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _employees = employees;
        _auditLogs = auditLogs;
    }

    public async Task<IActionResult> Index(int? month, int? year)
    {
        await LoadPageAsync("Users");

        var users = _userManager.Users.OrderBy(u => u.UserName).ToList();
        var items = new List<UserListItem>();
        foreach (var user in users)
        {
            items.Add(new UserListItem
            {
                Id = user.Id,
                Username = user.UserName ?? "",
                Email = user.Email,
                Roles = (await _userManager.GetRolesAsync(user)).ToList(),
                CreatedAt = user.CreatedAt,
                AssignedEmployeeName = (await _employees.GetByUserIdAsync(user.Id))?.Name
            });
        }

        if (month.HasValue)
            items = items.Where(u => u.CreatedAt.Month == month.Value).ToList();
        if (year.HasValue)
            items = items.Where(u => u.CreatedAt.Year == year.Value).ToList();

        ViewData["CurrentMonth"] = month;
        ViewData["CurrentYear"] = year;

        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewData["AllRoles"] = await GetAllRoleNamesAsync();
        return View(new CreateUserRequest());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserRequest request)
    {
        var allRoleNames = await GetAllRoleNamesAsync();
        ViewData["AllRoles"] = allRoleNames;

        if (!ModelState.IsValid)
            return View(request);

        var user = new User
        {
            UserName = request.Username,
            Email = request.Email,
            CreatedAt = DateTime.UtcNow,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(request);
        }

        var roles = request.SelectedRoles.Intersect(allRoleNames).ToList();
        if (roles.Count > 0)
            await _userManager.AddToRolesAsync(user, roles);

        await _auditLogs.LogAsync("User", "Add", user.Id.ToString(), CurrentUserId, CurrentUsername,
            $"Created user '{user.UserName}' with roles [{string.Join(", ", roles)}]",
            newValues: AuditJson(new { user.UserName, user.Email, Roles = roles }));

        TempData["Success"] = $"User '{user.UserName}' created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
            return NotFound();

        ViewData["AllRoles"] = await GetAllRoleNamesAsync();
        var currentRoles = await _userManager.GetRolesAsync(user);
        return View(new EditUserRequest
        {
            Id = user.Id,
            Username = user.UserName ?? "",
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            SelectedRoles = currentRoles.ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditUserRequest request)
    {
        var allRoleNames = await GetAllRoleNamesAsync();
        ViewData["AllRoles"] = allRoleNames;

        if (id != request.Id)
            return BadRequest();

        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
            return NotFound();

        if (!ModelState.IsValid)
            return View(request);

        var currentRoles = await _userManager.GetRolesAsync(user);
        var requestedRoles = request.SelectedRoles.Intersect(allRoleNames).ToList();

        if (currentRoles.Contains(RoleNames.Admin) && !requestedRoles.Contains(RoleNames.Admin)
            && await IsLastAdminAsync(user))
        {
            ModelState.AddModelError(string.Empty, "Cannot remove the Admin role from the last remaining Admin user.");
            return View(request);
        }

        var before = new { user.Email, user.PhoneNumber, Roles = currentRoles };

        user.Email = request.Email;
        user.PhoneNumber = request.PhoneNumber;
        await _userManager.UpdateAsync(user);

        var toAdd = requestedRoles.Except(currentRoles).ToList();
        var toRemove = currentRoles.Except(requestedRoles).ToList();
        if (toAdd.Count > 0)
            await _userManager.AddToRolesAsync(user, toAdd);
        if (toRemove.Count > 0)
            await _userManager.RemoveFromRolesAsync(user, toRemove);

        await _auditLogs.LogAsync("User", "Update", user.Id.ToString(), CurrentUserId, CurrentUsername,
            $"Updated user '{user.UserName}'", oldValues: AuditJson(before),
            newValues: AuditJson(new { user.Email, user.PhoneNumber, Roles = requestedRoles }));

        TempData["Success"] = $"User '{user.UserName}' updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(int id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
            return NotFound();

        return View(new ResetPasswordRequest { Id = user.Id, Username = user.UserName ?? "" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        var user = await _userManager.FindByIdAsync(request.Id.ToString());
        if (user is null)
            return NotFound();

        if (!ModelState.IsValid)
            return View(request);

        // Not Remove+Add: PasswordHash is NOT NULL, so the intermediate "removed" state fails to save.
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetResult = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);
        if (!resetResult.Succeeded)
        {
            foreach (var error in resetResult.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(request);
        }

        await _auditLogs.LogAsync("User", "Update", user.Id.ToString(), CurrentUserId, CurrentUsername,
            $"Reset password for '{user.UserName}'.");

        TempData["Success"] = $"Password reset for '{user.UserName}'.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
            return NotFound();

        if (await _userManager.IsInRoleAsync(user, RoleNames.Admin) && await IsLastAdminAsync(user))
        {
            TempData["Error"] = "Cannot delete the last remaining Admin user.";
            return RedirectToAction(nameof(Index));
        }

        var roles = await _userManager.GetRolesAsync(user);
        await _userManager.DeleteAsync(user);

        await _auditLogs.LogAsync("User", "Delete", user.Id.ToString(), CurrentUserId, CurrentUsername,
            $"Deleted user '{user.UserName}'.", oldValues: AuditJson(new { user.UserName, user.Email, Roles = roles }));

        TempData["Success"] = $"User '{user.UserName}' deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> IsLastAdminAsync(User user)
    {
        var admins = await _userManager.GetUsersInRoleAsync(RoleNames.Admin);
        return admins.Count == 1 && admins[0].Id == user.Id;
    }

    /// <summary>All roles, dynamic — includes any custom roles created via the Role Manager, not just the 4 built-in names.</summary>
    private async Task<List<string>> GetAllRoleNamesAsync()
        => await _roleManager.Roles.OrderBy(r => r.Name).Select(r => r.Name!).ToListAsync();

    // --- JSON API used by the ported Areas/Admin/Views/Users/Index.cshtml assignment modal ---
    // Customers are intentionally not assignable here — customers don't get accounts.

    [HttpGet("/api/users/{id:int}/assign")]
    public async Task<IActionResult> GetAssignmentJson(int id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
            return Json(new { status = "error", error = "User not found." });

        var allEmployees = await _employees.GetAllAsync();
        var currentEmployee = await _employees.GetByUserIdAsync(id);

        return Json(new
        {
            status = "success",
            user = new { id = user.Id, userName = user.UserName, email = user.Email },
            availableEmployees = allEmployees.Select(e => new { id = e.Id, name = e.Name }),
            currentEmployee = currentEmployee is null ? null : new { id = currentEmployee.Id, name = currentEmployee.Name }
        });
    }

    [HttpPut("/api/users/{id:int}/assign-employee")]
    public async Task<IActionResult> AssignEmployeeJson(int id, [FromBody] AssignmentRequest request)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
            return Json(new { status = "error", error = "User not found." });

        var before = await _employees.GetByUserIdAsync(id);

        await _employees.AssignUserAsync(request.EmployeeId, id);

        await _auditLogs.LogAsync("User", "Update", id.ToString(), CurrentUserId, CurrentUsername,
            $"Assigned employee {request.EmployeeId?.ToString() ?? "none"} to user '{user.UserName}'.",
            oldValues: AuditJson(new { EmployeeId = before?.Id }), newValues: AuditJson(new { request.EmployeeId }));

        return Json(new { status = "success" });
    }
}
