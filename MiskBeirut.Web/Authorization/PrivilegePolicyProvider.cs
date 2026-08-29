using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace MiskBeirut.Web.Authorization;

/// <summary>
/// Synthesizes an authorization policy on demand for any "Privilege:{key}" policy name — so
/// <see cref="RequirePrivilegeAttribute"/> works for arbitrary, database-defined privilege keys
/// without pre-registering a policy per page in Program.cs. Falls back to the default provider
/// for every other policy name (e.g. ASP.NET Identity's built-in ones).
/// </summary>
public class PrivilegePolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public PrivilegePolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallback = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(RequirePrivilegeAttribute.PolicyPrefix, StringComparison.Ordinal))
        {
            var key = policyName[RequirePrivilegeAttribute.PolicyPrefix.Length..];
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PrivilegeRequirement(key))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallback.GetPolicyAsync(policyName);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();
}
