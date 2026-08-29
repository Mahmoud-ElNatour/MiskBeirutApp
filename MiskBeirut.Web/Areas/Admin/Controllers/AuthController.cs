using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MiskBeirut.Application.Managers;
using MiskBeirut.Core.Entities;
using MiskBeirut.Web.Areas.Admin.Models;

namespace MiskBeirut.Web.Areas.Admin.Controllers;

[AllowAnonymous]
public class AuthController : AdminControllerBase
{
    private readonly SignInManager<User> _signInManager;

    public AuthController(SignInManager<User> signInManager, BackofficePageContentManager pages) : base(pages)
    {
        _signInManager = signInManager;
    }

    [HttpGet]
    public async Task<IActionResult> Login()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        await LoadPageAsync("Login");
        return View(new LoginRequest());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        await LoadPageAsync("Login");

        if (!ModelState.IsValid)
            return View(request);

        var result = await _signInManager.PasswordSignInAsync(request.Username, request.Password, isPersistent: true, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return View(request);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    [HttpGet]
    public IActionResult Signup()
    {
        // TODO: port old SignupPost logic (see Areas/Admin/_Legacy/Controllers/AuthController.cs) once wired to new User/Identity setup.
        return View();
    }
}
