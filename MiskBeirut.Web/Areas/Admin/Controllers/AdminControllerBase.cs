using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiskBeirut.Core.Constants;

namespace MiskBeirut.Web.Areas.Admin.Controllers;

/// <summary>
/// Base for every Admin-area controller. Grants entry to everyone who has *some* reason to be
/// in the Admin area (Admin, Supervisor, Employee) — Content is Cms-only and never lands here.
/// Individual controllers/actions layer tighter, feature-specific restrictions on top of this
/// (e.g. Employee's Daily-Closing-write-only, Supervisor's payroll-read-only) once built.
/// </summary>
[Area("Admin")]
[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Supervisor},{RoleNames.Employee}")]
public abstract class AdminControllerBase : Controller
{
    protected int? CurrentUserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    protected string? CurrentUsername => User.Identity?.Name;
}
