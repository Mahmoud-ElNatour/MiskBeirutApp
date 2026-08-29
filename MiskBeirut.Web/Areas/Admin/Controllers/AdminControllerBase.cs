using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using MiskBeirut.Application.Managers;
using MiskBeirut.Web.Authorization;
using MiskBeirut.Web.Support;

namespace MiskBeirut.Web.Areas.Admin.Controllers;

/// <summary>
/// Base for every Admin-area controller. Grants entry to everyone who has *some* privilege at
/// all (Admin always passes — see PrivilegeAuthorizationHandler); a role with zero privileges
/// assigned — e.g. Content, which is Cms-only — never lands here. Individual controllers require
/// their own specific page/section privilege on top of this baseline via [RequirePrivilege("X")];
/// a handful of actions also layer a legacy hardcoded role check for within-page distinctions
/// that predate the privilege system (e.g. Employee's Daily-Closing-write-only, Supervisor's
/// payroll-read-only) — see each controller for specifics.
///
/// Also resolves this page's CMS content (backoffice.pages / backoffice.pageattributes) so
/// static copy — headings, blurbs, empty states, button labels — can come from the database
/// the same way the public Customer area's <see cref="PageContentManager"/> does, instead of
/// being hardcoded in Razor.
/// </summary>
[Area("Admin")]
[RequirePrivilege]
public abstract class AdminControllerBase : Controller
{
    private const string GlobalPageName = "Global";

    private readonly BackofficePageContentManager _pages;

    protected AdminControllerBase(BackofficePageContentManager pages)
    {
        _pages = pages;
    }

    protected int? CurrentUserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    protected string? CurrentUsername => User.Identity?.Name;

    /// <summary>
    /// Serializes a before/after snapshot (typically a DTO) for AuditLogManager.LogAsync's
    /// oldValues/newValues — every call site was passing these as null, which is why the Audit Logs
    /// page's Details modal always showed "N/A" for Old/New Values on Update/Delete/Add entries.
    /// </summary>
    protected static string? AuditJson(object? value) => value is null ? null : JsonSerializer.Serialize(value);

    /// <summary>
    /// The first ModelState validation error message (e.g. from a [Phone]/[StringLength] attribute
    /// on a JSON API request), for the legacy-ported modals' `{status, message}` error shape —
    /// they only ever show a single message, not a per-field list.
    /// </summary>
    protected string FirstModelError() =>
        ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Invalid request.";

    /// <summary>
    /// Loads the named backoffice page's content plus the shared "Global" page's content, and
    /// stashes it in ViewData["Content"] so views whose @model is a business DTO (not the content
    /// itself) can still reach it: <c>@{ var content = (BackofficePageContent)ViewData["Content"]!; }</c>
    /// </summary>
    protected async Task<BackofficePageContent> LoadPageAsync(string pageName, CancellationToken cancellationToken = default)
    {
        var page = await _pages.GetPageByNameAsync(pageName, cancellationToken);
        var global = await _pages.GetPageByNameAsync(GlobalPageName, cancellationToken);
        var content = new BackofficePageContent(page, global);
        ViewData["Content"] = content;
        return content;
    }
}
