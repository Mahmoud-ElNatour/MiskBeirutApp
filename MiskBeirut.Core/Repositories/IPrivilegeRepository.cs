using MiskBeirut.Core.Entities;

namespace MiskBeirut.Core.Repositories;

/// <summary>Privileges (page/section capabilities) and their assignment to roles.</summary>
public interface IPrivilegeRepository
{
    Task<IReadOnlyList<Privilege>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>The Keys of privileges granted directly to this role (does not expand section -> page implication).</summary>
    Task<IReadOnlyList<int>> GetGrantedPrivilegeIdsAsync(int roleId, CancellationToken cancellationToken = default);

    /// <summary>Replaces the full set of privileges granted to a role.</summary>
    Task SetRolePrivilegesAsync(int roleId, IEnumerable<int> privilegeIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// True if any of the given role names has been granted <paramref name="privilegeKey"/> directly,
    /// or has been granted the section privilege that owns it.
    /// </summary>
    Task<bool> AnyRoleHasPrivilegeAsync(IReadOnlyCollection<string> roleNames, string privilegeKey, CancellationToken cancellationToken = default);

    /// <summary>True if any of the given role names has been granted any privilege at all.</summary>
    Task<bool> AnyRoleHasAnyPrivilegeAsync(IReadOnlyCollection<string> roleNames, CancellationToken cancellationToken = default);

    Task DeleteRolePrivilegesAsync(int roleId, CancellationToken cancellationToken = default);
}
