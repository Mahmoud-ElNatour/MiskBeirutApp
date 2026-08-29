namespace MiskBeirut.Core.Entities;

/// <summary>
/// backoffice.privileges — one row per grantable Admin-area capability. A privilege is either a
/// whole Control Panel section (IsSection = true, SectionKey = null) or a single page within a
/// section (IsSection = false, SectionKey = the owning section's Key). Granting a role a section
/// privilege implies every page privilege under that section — see <c>PrivilegeManager</c>.
/// </summary>
public class Privilege
{
    public int Id { get; set; }
    public string Key { get; set; } = null!;
    public string Name { get; set; } = null!;
    public bool IsSection { get; set; }
    public string? SectionKey { get; set; }

    public ICollection<RolePrivilege> RolePrivileges { get; set; } = new List<RolePrivilege>();
}
