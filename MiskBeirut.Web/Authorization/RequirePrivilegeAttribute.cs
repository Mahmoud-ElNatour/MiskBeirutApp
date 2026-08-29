using Microsoft.AspNetCore.Authorization;

namespace MiskBeirut.Web.Authorization;

/// <summary>
/// Gates a controller/action behind a dynamic, database-managed privilege instead of a hardcoded
/// role name. Resolved at runtime by <see cref="PrivilegePolicyProvider"/> +
/// <see cref="PrivilegeAuthorizationHandler"/>. Use the parameterless form
/// (<c>[RequirePrivilege]</c>) to require "any privilege at all" — the baseline gate for pages
/// every privileged Admin-area user should reach (e.g. the dashboard), as opposed to a specific
/// page like <c>[RequirePrivilege("Employees")]</c>.
/// </summary>
public class RequirePrivilegeAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "Privilege:";

    public RequirePrivilegeAttribute(string key = PrivilegeRequirement.Any)
    {
        Policy = PolicyPrefix + key;
    }
}
