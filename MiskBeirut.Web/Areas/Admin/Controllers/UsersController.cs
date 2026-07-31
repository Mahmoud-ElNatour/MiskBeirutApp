using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MiskBeirut.Application.Managers;
using MiskBeirut.Core.Constants;
using MiskBeirut.Core.Entities;
using MiskBeirut.Web.Areas.Admin.Models.Users;

namespace MiskBeirut.Web.Areas.Admin.Controllers;

[Authorize(Roles = RoleNames.Admin)]
public class UsersController : AdminControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole<int>> _roleManager;
    private readonly AuditLogManager _auditLogs;

    public UsersController(UserManager<User> userManager, RoleManager<IdentityRole<int>> roleManager, AuditLogManager auditLogs)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _auditLogs = auditLogs;
    }

    public async Task<IActionResult> Index()
    {
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
                CreatedAt = user.CreatedAt
            });
        }

        return View(items);
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewData["AllRoles"] = RoleNames.All;
        return View(new CreateUserRequest());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserRequest request)
    {
        ViewData["AllRoles"] = RoleNames.All;

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

        var roles = request.SelectedRoles.Intersect(RoleNames.All).ToList();
        if (roles.Count > 0)
            await _userManager.AddToRolesAsync(user, roles);

        await _auditLogs.LogAsync("User", "Add", user.Id.ToString(), CurrentUserId, CurrentUsername,
            $"Created user '{user.UserName}' with roles [{string.Join(", ", roles)}]");

        TempData["Success"] = $"User '{user.UserName}' created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
            return NotFound();

        ViewData["AllRoles"] = RoleNames.All;
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
        ViewData["AllRoles"] = RoleNames.All;

        if (id != request.Id)
            return BadRequest();

        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
            return NotFound();

        if (!ModelState.IsValid)
            return View(request);

        var currentRoles = await _userManager.GetRolesAsync(user);
        var requestedRoles = request.SelectedRoles.Intersect(RoleNames.All).ToList();

        if (currentRoles.Contains(RoleNames.Admin) && !requestedRoles.Contains(RoleNames.Admin)
            && await IsLastAdminAsync(user))
        {
            ModelState.AddModelError(string.Empty, "Cannot remove the Admin role from the last remaining Admin user.");
            return View(request);
        }

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
            $"Updated user '{user.UserName}'", oldValues: string.Join(",", currentRoles), newValues: string.Join(",", requestedRoles));

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

        await _userManager.DeleteAsync(user);

        await _auditLogs.LogAsync("User", "Delete", user.Id.ToString(), CurrentUserId, CurrentUsername,
            $"Deleted user '{user.UserName}'.");

        TempData["Success"] = $"User '{user.UserName}' deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> IsLastAdminAsync(User user)
    {
        var admins = await _userManager.GetUsersInRoleAsync(RoleNames.Admin);
        return admins.Count == 1 && admins[0].Id == user.Id;
    }
}
