using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MiskBeirut.Core.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MiskBeirut.Web.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class SettingsController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ILogger<SettingsController> _logger;

        public SettingsController(UserManager<User> userManager, SignInManager<User> signInManager, ILogger<SettingsController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        // GET: /settings
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("Unauthorized settings view request");
                return Challenge();
            }
            return View(user);
        }

        // POST: /settings
        [HttpPost]
        public async Task<IActionResult> Update([FromForm] string? username, [FromForm] string? email, 
            [FromForm] string? current_password, [FromForm] string? new_password, [FromForm] string? confirm_password)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("Unauthorized settings update request");
                return Challenge();
            }

            // Check if it's password update or profile update
            if (!string.IsNullOrEmpty(current_password))
            {
                // Password Update
                _logger.LogInformation("Updating User ID {ResourceId} password", user.Id);
                if (string.IsNullOrEmpty(new_password) || new_password != confirm_password)
                {
                    _logger.LogWarning("Password update failed: Passwords do not match or are empty for User ID {ResourceId}", user.Id);
                    TempData["Error"] = "Passwords do not match or are empty.";
                    return View("Index", user);
                }

                var result = await _userManager.ChangePasswordAsync(user, current_password, new_password);
                if (result.Succeeded)
                {
                    await _signInManager.RefreshSignInAsync(user);
                    _logger.LogInformation("Successfully updated User ID {ResourceId} password", user.Id);
                    TempData["Success"] = "Password updated successfully.";
                }
                else
                {
                    var errors = string.Join(" ", result.Errors.Select(e => e.Description));
                    _logger.LogWarning("Failed to update User ID {ResourceId} password: {Errors}", user.Id, errors);
                    TempData["Error"] = errors;
                }
            }
            else
            {
                // Profile Update
                _logger.LogInformation("Updating User ID {ResourceId} profile to data: {@RequestData}", user.Id, new { username, email });
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email))
                {
                    _logger.LogWarning("Profile update failed: Username and Email cannot be empty for User ID {ResourceId}", user.Id);
                    TempData["Error"] = "Username and Email cannot be empty.";
                    return View("Index", user);
                }

                // Check if username/email already taken by another user
                var existingUserByName = await _userManager.FindByNameAsync(username);
                if (existingUserByName != null && existingUserByName.Id != user.Id)
                {
                    _logger.LogWarning("Profile update failed: Username is already taken by another user for User ID {ResourceId}", user.Id);
                    TempData["Error"] = "Username is already taken.";
                    return View("Index", user);
                }

                var existingUserByEmail = await _userManager.FindByEmailAsync(email);
                if (existingUserByEmail != null && existingUserByEmail.Id != user.Id)
                {
                    _logger.LogWarning("Profile update failed: Email is already taken by another user for User ID {ResourceId}", user.Id);
                    TempData["Error"] = "Email is already taken.";
                    return View("Index", user);
                }

                user.UserName = username;
                user.Email = email;

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    await _signInManager.RefreshSignInAsync(user);
                    _logger.LogInformation("Successfully updated User ID {ResourceId} profile", user.Id);
                    TempData["Success"] = "Profile updated successfully.";
                }
                else
                {
                    var errors = string.Join(" ", result.Errors.Select(e => e.Description));
                    _logger.LogWarning("Failed to update User ID {ResourceId} profile: {Errors}", user.Id, errors);
                    TempData["Error"] = errors;
                }
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
