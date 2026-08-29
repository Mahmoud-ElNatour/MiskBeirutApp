using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiskBeirut.Core.Constants;

namespace MiskBeirut.Web.Areas.Cms.Controllers;

/// <summary>
/// Base for every Cms-area controller. Content-management only (no financial data), so unlike the
/// Admin area this uses a simple role gate rather than the page/section privilege system —
/// anyone who can reach the Cms area at all (Admin or Content) can manage all of its content.
/// </summary>
[Area("Cms")]
[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Content}")]
public abstract class CmsControllerBase : Controller
{
    protected int? CurrentUserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    protected string? CurrentUsername => User.Identity?.Name;
}
