using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using MiskBeirut.Application.Managers;
using MiskBeirut.Core.Constants;

namespace MiskBeirut.Web.Authorization;

/// <summary>
/// Admin always succeeds (safety net — a misconfigured privilege set can never lock every admin
/// out). Every other role is checked against the database: does any role the user holds grant
/// this privilege directly, or grant the section privilege that owns it?
/// </summary>
public class PrivilegeAuthorizationHandler : AuthorizationHandler<PrivilegeRequirement>
{
    private readonly PrivilegeManager _privileges;

    public PrivilegeAuthorizationHandler(PrivilegeManager privileges)
    {
        _privileges = privileges;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PrivilegeRequirement requirement)
    {
        if (context.User.IsInRole(RoleNames.Admin))
        {
            context.Succeed(requirement);
            return;
        }

        var roleNames = context.User.FindAll(ClaimTypes.Role).Select(c => c.Value).Distinct().ToList();
        if (roleNames.Count == 0)
            return;

        var granted = requirement.Key == PrivilegeRequirement.Any
            ? await _privileges.AnyRoleHasAnyPrivilegeAsync(roleNames)
            : await _privileges.AnyRoleHasPrivilegeAsync(roleNames, requirement.Key);

        if (granted)
            context.Succeed(requirement);
    }
}
