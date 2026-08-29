using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MiskBeirut.Application.Managers;
using MiskBeirut.Core.Entities;
using MiskBeirut.Web.Areas.Admin.Models.Settings;

namespace MiskBeirut.Web.Areas.Admin.Controllers;

public class SettingsController : AdminControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly AuditLogManager _auditLogs;

    public SettingsController(UserManager<User> userManager, AuditLogManager auditLogs, BackofficePageContentManager pages) : base(pages)
    {
        _userManager = userManager;
        _auditLogs = auditLogs;
    }

    public async Task<IActionResult> Index()
    {
        await LoadPageAsync("Settings");

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Challenge();

        var roles = await _userManager.GetRolesAsync(user);
        return View(new SettingsViewModel { User = user, Roles = roles.ToList() });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(UpdateSettingsRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Challenge();

        var changingPassword = !string.IsNullOrWhiteSpace(request.NewPassword);
        var before = new { user.UserName, user.Email };

        if (changingPassword)
        {
            if (request.NewPassword != request.ConfirmPassword)
            {
                TempData["Error"] = "New password and confirmation do not match.";
                return RedirectToAction(nameof(Index));
            }

            var passwordResult = await _userManager.ChangePasswordAsync(user, request.CurrentPassword ?? "", request.NewPassword!);
            if (!passwordResult.Succeeded)
            {
                TempData["Error"] = string.Join(" ", passwordResult.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Index));
            }

            await _auditLogs.LogAsync("User", "Update", user.Id.ToString(), CurrentUserId, CurrentUsername, "Changed own password.");
        }

        if (!string.IsNullOrWhiteSpace(request.Username) && request.Username != user.UserName)
        {
            var usernameResult = await _userManager.SetUserNameAsync(user, request.Username);
            if (!usernameResult.Succeeded)
            {
                TempData["Error"] = string.Join(" ", usernameResult.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Index));
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != user.Email)
        {
            var emailResult = await _userManager.SetEmailAsync(user, request.Email);
            if (!emailResult.Succeeded)
            {
                TempData["Error"] = string.Join(" ", emailResult.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Index));
            }
        }

        await _auditLogs.LogAsync("User", "Update", user.Id.ToString(), CurrentUserId, CurrentUsername, "Updated own profile settings.",
            oldValues: AuditJson(before), newValues: AuditJson(new { user.UserName, user.Email }));
        TempData["Success"] = changingPassword ? "Password updated." : "Profile updated.";
        return RedirectToAction(nameof(Index));
    }
}