using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MiskBeirut.Application.DTOs;
using MiskBeirut.Application.Services;
using MiskBeirut.Core.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MiskBeirut.Web.Controllers
{
    [Route("[controller]")]
    public class AuthController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ILogger<AuthController> _logger;

        public AuthController(UserManager<User> userManager, SignInManager<User> signInManager, ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        // GET: /Auth/Login
        [HttpGet("Login")]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return Redirect("/");
            }
            ViewData["Title"] = "Log In";
            return View();
        }

        // POST: /Auth/Login
        [HttpPost("Login")]
        public async Task<IActionResult> LoginPost([FromForm] LoginRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Invalid details provided.";
                return View("Login");
            }

            var username = request.UsernameOrEmail;
            _logger.LogInformation("Retrieving User for authentication: {Username}", username);

            if (username.Contains("@"))
            {
                var userByEmail = await _userManager.FindByEmailAsync(username);
                if (userByEmail != null)
                {
                    username = userByEmail.UserName ?? username;
                }
            }

            var result = await _signInManager.PasswordSignInAsync(username, request.Password, isPersistent: true, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                _logger.LogInformation("Successfully authenticated User {Username}", username);
                TempData["Success"] = $"Welcome back, {username}!";
                return Redirect("/");
            }

            _logger.LogWarning("Invalid password or username attempt for User {Username}", request.UsernameOrEmail);
            TempData["Error"] = "Invalid username/email or password.";
            return View("Login");
        }

        // GET: /Auth/Signup
        [HttpGet("Signup")]
        public IActionResult Signup()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return Redirect("/");
            }
            ViewData["Title"] = "Sign Up";
            return View();
        }

        // POST: /Auth/Signup
        [HttpPost("Signup")]
        public async Task<IActionResult> SignupPost([FromForm] RegisterRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Invalid registration details.";
                return View("Signup");
            }

            _logger.LogInformation("Creating new User with data: {@RequestData}", new { request.Username, request.Email });

            var user = new User
            {
                UserName = request.Username,
                Email = request.Email,
                Role = "user",
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (result.Succeeded)
            {
                _logger.LogInformation("Successfully created User with username: {ResourceId}", user.UserName);
                TempData["Success"] = "Account created successfully! Please log in.";
                return RedirectToAction(nameof(Login));
            }

            var errorMsg = string.Join(" ", result.Errors.Select(e => e.Description));
            _logger.LogWarning("Failed to create User {Username} with errors: {Errors}", request.Username, errorMsg);
            TempData["Error"] = errorMsg;
            return View("Signup");
        }

        // GET: /Auth/Logout
        [HttpGet("Logout")]
        public async Task<IActionResult> Logout()
        {
            _logger.LogInformation("Initiating logout for current user");
            await _signInManager.SignOutAsync();
            _logger.LogInformation("Successfully logged out user");
            TempData["Success"] = "Logged out successfully.";
            return RedirectToAction(nameof(Login));
        }
    }
}
