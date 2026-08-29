using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MiskBeirut.Core.Constants;
using MiskBeirut.Core.Entities;
using MiskBeirut.Infrastructure.DbContexts;

namespace MiskBeirut.Web.Support;

/// <summary>
/// Ensures every page/section privilege exists (idempotent — new privileges get added here as new
/// Admin pages ship) and that Supervisor/Employee keep the same reach they had before the
/// privilege system replaced their hardcoded [Authorize(Roles = "...")] gates. Runs after
/// <see cref="AdminSeeder"/> so the roles it creates already exist. The Admin role needs no rows
/// here — it always has full access (see PrivilegeAuthorizationHandler).
/// </summary>
public static class PrivilegeSeeder
{
    private const string Operations = "Operations";
    private const string Investors = "Investors";
    private const string Administration = "Administration";

    private static readonly (string Key, string Name, string? SectionKey)[] Privileges =
    [
        (Operations, "Operations", null),
        (Investors, "Investors", null),
        (Administration, "Administration", null),

        ("DailyClosing", "Daily Closing", Operations),
        ("Employees", "Employees", Operations),
        ("Customers", "Customers", Operations),
        ("Payroll", "Payroll", Operations),
        ("Credits", "Credits", Operations),
        ("Cashbacks", "Cashbacks", Operations),
        ("DeductionsAdvances", "Deductions & Advances", Operations),
        ("Expenses", "Expenses", Operations),
        ("Receivers", "Receivers", Operations),

        // Investors has exactly one page (itself), so its section privilege doubles as the page
        // gate directly — no separate page-level "Investors" row (that would collide on Key with
        // the section row above; see IX_privileges_Key).

        ("Reports", "Reports", Administration),
        ("Users", "Users", Administration),
        ("AuditLogs", "Audit Logs", Administration),
        ("RoleManager", "Role Manager", Administration)
    ];

    /// <summary>Privileges granted to Supervisor/Employee by default, matching their pre-privilege-system access.</summary>
    private static readonly Dictionary<string, string[]> DefaultRoleGrants = new()
    {
        [RoleNames.Supervisor] = ["DailyClosing", "Customers", "Payroll", "Credits", "Cashbacks", "Expenses"],
        [RoleNames.Employee] = ["DailyClosing"]
    };

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MiskBeirutDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();

        // A page privilege can share its Key with its Investors-section peer above by design
        // (Investors is both the section and its sole page) — de-dupe by (Key, IsSection).
        var existing = await context.Privileges.ToListAsync();
        foreach (var (key, name, sectionKey) in Privileges)
        {
            var isSection = sectionKey is null;
            if (existing.Any(p => p.Key == key && p.IsSection == isSection))
                continue;

            context.Privileges.Add(new Privilege { Key = key, Name = name, IsSection = isSection, SectionKey = sectionKey });
        }

        await context.SaveChangesAsync();

        var allPrivileges = await context.Privileges.ToListAsync();

        foreach (var (roleName, grantKeys) in DefaultRoleGrants)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null)
                continue;

            var alreadyGranted = await context.RolePrivileges.Where(rp => rp.RoleId == role.Id).Select(rp => rp.PrivilegeId).ToListAsync();

            foreach (var key in grantKeys)
            {
                var privilege = allPrivileges.FirstOrDefault(p => p.Key == key && !p.IsSection);
                if (privilege is null || alreadyGranted.Contains(privilege.Id))
                    continue;

                context.RolePrivileges.Add(new RolePrivilege { RoleId = role.Id, PrivilegeId = privilege.Id });
            }
        }

        await context.SaveChangesAsync();
    }
}
