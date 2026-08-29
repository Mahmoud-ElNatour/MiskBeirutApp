using MiskBeirut.Application.Dtos.Privileges;
using MiskBeirut.Core.Repositories;

namespace MiskBeirut.Application.Managers;

/// <summary>
/// Page/section privileges and their assignment to roles. The Admin role always has full access
/// and is never checked here — enforced by the caller (see PrivilegeAuthorizationHandler) before
/// this manager is consulted.
/// </summary>
public class PrivilegeManager
{
    private readonly IPrivilegeRepository _privileges;

    public PrivilegeManager(IPrivilegeRepository privileges)
    {
        _privileges = privileges;
    }

    public async Task<IReadOnlyList<PrivilegeDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var privileges = await _privileges.GetAllAsync(cancellationToken);
        return privileges.Select(p => new PrivilegeDto
        {
            Id = p.Id,
            Key = p.Key,
            Name = p.Name,
            IsSection = p.IsSection,
            SectionKey = p.SectionKey
        }).ToList();
    }

    public Task<IReadOnlyList<int>> GetGrantedPrivilegeIdsAsync(int roleId, CancellationToken cancellationToken = default)
        => _privileges.GetGrantedPrivilegeIdsAsync(roleId, cancellationToken);

    public Task SetRolePrivilegesAsync(int roleId, IEnumerable<int> privilegeIds, CancellationToken cancellationToken = default)
        => _privileges.SetRolePrivilegesAsync(roleId, privilegeIds, cancellationToken);

    public Task DeleteRolePrivilegesAsync(int roleId, CancellationToken cancellationToken = default)
        => _privileges.DeleteRolePrivilegesAsync(roleId, cancellationToken);

    public Task<bool> AnyRoleHasPrivilegeAsync(IReadOnlyCollection<string> roleNames, string privilegeKey, CancellationToken cancellationToken = default)
        => _privileges.AnyRoleHasPrivilegeAsync(roleNames, privilegeKey, cancellationToken);

    public Task<bool> AnyRoleHasAnyPrivilegeAsync(IReadOnlyCollection<string> roleNames, CancellationToken cancellationToken = default)
        => _privileges.AnyRoleHasAnyPrivilegeAsync(roleNames, cancellationToken);
}
