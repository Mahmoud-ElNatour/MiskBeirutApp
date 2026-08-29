using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using MiskBeirut.Application.Managers;
using MiskBeirut.Core.Constants;

namespace MiskBeirut.Web.Areas.Admin.ViewComponents;

/// <summary>
/// One sidebar nav item — Controller/Action it links to, and the privilege key that must be
/// granted to show it (null = visible to anyone who can reach the Admin area at all). Category
/// null means the item renders top-level, outside any collapsible group (Dashboard only) — every
/// other item belongs to one of the two groups the sidebar renders as an accordion section
/// (mirrors ControlPanel.cshtml's own "Operations"/"Administration" grouping).
/// </summary>
public sealed record AdminNavItem(string Label, string Icon, string Controller, string Action, string? PrivilegeKey, string? Category);

/// <summary>
/// Sidebar's nav list (mirrors Cms's PagesNavViewComponent) — privilege-driven, not role-driven,
/// so it always matches what each controller actually enforces via [RequirePrivilege("X")] (see
/// AdminControllerBase, PrivilegeAuthorizationHandler) instead of the old top nav's hardcoded
/// User.IsInRole checks, which had drifted from that (e.g. Investors/Expenses/Reports/Logs/
/// Settings were reachable but had no nav link at all). Admin sees every item unconditionally,
/// mirroring the handler's own Admin bypass; everyone else only sees items whose privilege key
/// is granted to at least one role they hold.
/// </summary>
public class AdminNavViewComponent : ViewComponent
{
    private const string Operations = "Operations";
    private const string Administration = "Administration";

    private static readonly IReadOnlyList<AdminNavItem> AllItems =
    [
        new("Dashboard", "dashboard", "Home", "Index", null, null),

        new("Daily Closing", "point_of_sale", "DailyClosing", "Index", "DailyClosing", Operations),
        new("Customers", "groups", "Customers", "Index", "Customers", Operations),
        new("Credits", "account_balance_wallet", "Customers", "Credits", "Credits", Operations),
        new("Cashbacks", "redeem", "Customers", "Cashbacks", "Cashbacks", Operations),
        new("Employees", "badge", "Employees", "Index", "Employees", Operations),
        new("Deductions & Advances", "request_quote", "Employees", "DeductionsAdvances", "DeductionsAdvances", Operations),
        new("Payroll", "payments", "Payroll", "Index", "Payroll", Operations),
        new("Receivers", "call_received", "Receivers", "Index", "Receivers", Operations),
        new("Investors", "trending_up", "Investors", "Index", "Investors", Operations),
        new("Expenses", "receipt_long", "Expenses", "Index", "Expenses", Operations),

        new("Reports", "bar_chart", "System", "Reports", "Reports", Administration),
        new("Users", "manage_accounts", "Users", "Index", "Users", Administration),
        new("Role Manager", "admin_panel_settings", "System", "Roles", "RoleManager", Administration),
        new("Audit Logs", "history", "AuditLogs", "Index", "AuditLogs", Administration),
        new("Control Panel", "settings_applications", "System", "ControlPanel", null, Administration),
        new("Settings", "settings", "Settings", "Index", null, Administration)
    ];

    private readonly PrivilegeManager _privileges;

    public AdminNavViewComponent(PrivilegeManager privileges)
    {
        _privileges = privileges;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var isAdmin = User.IsInRole(RoleNames.Admin);

        // Employee (and no Supervisor/Admin role) gets the "Submit Closing" label/route instead
        // of full Daily Closing management — the one legacy nuance kept on top of the privilege
        // check, matching what DailyClosingController itself still enforces.
        ViewBag.IsEmployeeOnly = !isAdmin && User.IsInRole(RoleNames.Employee) && !User.IsInRole(RoleNames.Supervisor);

        if (isAdmin)
            return View(AllItems.ToList());

        var roleNames = ((ClaimsPrincipal)User).FindAll(ClaimTypes.Role).Select(c => c.Value).Distinct().ToList();
        if (roleNames.Count == 0)
            return View(new List<AdminNavItem>());

        var checks = await Task.WhenAll(AllItems.Select(async item => (
            item,
            granted: item.PrivilegeKey is null || await _privileges.AnyRoleHasPrivilegeAsync(roleNames, item.PrivilegeKey))));

        return View(checks.Where(c => c.granted).Select(c => c.item).ToList());
    }
}
