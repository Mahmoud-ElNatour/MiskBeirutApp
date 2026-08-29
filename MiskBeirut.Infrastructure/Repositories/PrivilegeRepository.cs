using Microsoft.EntityFrameworkCore;
using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Repositories;
using MiskBeirut.Infrastructure.DbContexts;

namespace MiskBeirut.Infrastructure.Repositories;

public class PrivilegeRepository : IPrivilegeRepository
{
    private readonly MiskBeirutDbContext _context;

    public PrivilegeRepository(MiskBeirutDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Privilege>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.Privileges
            .AsNoTracking()
            .OrderBy(p => p.SectionKey ?? p.Key)
            .ThenBy(p => p.IsSection ? 0 : 1)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<int>> GetGrantedPrivilegeIdsAsync(int roleId, CancellationToken cancellationToken = default)
        => await _context.RolePrivileges
            .AsNoTracking()
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.PrivilegeId)
            .ToListAsync(cancellationToken);

    public async Task SetRolePrivilegesAsync(int roleId, IEnumerable<int> privilegeIds, CancellationToken cancellationToken = default)
    {
        var existing = await _context.RolePrivileges.Where(rp => rp.RoleId == roleId).ToListAsync(cancellationToken);
        _context.RolePrivileges.RemoveRange(existing);

        foreach (var privilegeId in privilegeIds.Distinct())
        {
            _context.RolePrivileges.Add(new RolePrivilege { RoleId = roleId, PrivilegeId = privilegeId });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteRolePrivilegesAsync(int roleId, CancellationToken cancellationToken = default)
    {
        var existing = await _context.RolePrivileges.Where(rp => rp.RoleId == roleId).ToListAsync(cancellationToken);
        _context.RolePrivileges.RemoveRange(existing);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> AnyRoleHasPrivilegeAsync(IReadOnlyCollection<string> roleNames, string privilegeKey, CancellationToken cancellationToken = default)
    {
        if (roleNames.Count == 0)
            return false;

        var roleIds = await _context.Roles
            .Where(r => r.Name != null && roleNames.Contains(r.Name))
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        if (roleIds.Count == 0)
            return false;

        var privilege = await _context.Privileges.AsNoTracking().FirstOrDefaultAsync(p => p.Key == privilegeKey, cancellationToken);
        if (privilege is null)
            return false;

        // Granted directly, or via the owning section's privilege.
        var relevantPrivilegeIds = new List<int> { privilege.Id };
        if (!privilege.IsSection && privilege.SectionKey is not null)
        {
            var sectionPrivilegeId = await _context.Privileges
                .AsNoTracking()
                .Where(p => p.IsSection && p.Key == privilege.SectionKey)
                .Select(p => (int?)p.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (sectionPrivilegeId.HasValue)
                relevantPrivilegeIds.Add(sectionPrivilegeId.Value);
        }

        return await _context.RolePrivileges
            .AsNoTracking()
            .AnyAsync(rp => roleIds.Contains(rp.RoleId) && relevantPrivilegeIds.Contains(rp.PrivilegeId), cancellationToken);
    }

    public async Task<bool> AnyRoleHasAnyPrivilegeAsync(IReadOnlyCollection<string> roleNames, CancellationToken cancellationToken = default)
    {
        if (roleNames.Count == 0)
            return false;

        var roleIds = await _context.Roles
            .Where(r => r.Name != null && roleNames.Contains(r.Name))
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        if (roleIds.Count == 0)
            return false;

        return await _context.RolePrivileges.AsNoTracking().AnyAsync(rp => roleIds.Contains(rp.RoleId), cancellationToken);
    }
}
