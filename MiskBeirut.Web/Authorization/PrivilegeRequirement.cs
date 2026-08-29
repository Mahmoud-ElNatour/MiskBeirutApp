using Microsoft.AspNetCore.Authorization;

namespace MiskBeirut.Web.Authorization;

/// <summary>
/// Requires the current user's roles to collectively grant the named page privilege (or, if
/// Key is <see cref="Any"/>, any privilege at all). The Admin role always satisfies this —
/// enforced by <see cref="PrivilegeAuthorizationHandler"/>, not represented here.
/// </summary>
public class PrivilegeRequirement : IAuthorizationRequirement
{
    /// <summary>Sentinel key meaning "has at least one privilege" rather than a specific page/section.</summary>
    public const string Any = "*";

    public string Key { get; }

    public PrivilegeRequirement(string key)
    {
        Key = key;
    }
}
