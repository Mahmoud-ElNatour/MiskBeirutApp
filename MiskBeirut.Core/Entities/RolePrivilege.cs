namespace MiskBeirut.Core.Entities;

/// <summary>backoffice.role_privileges — join between an Identity role and a granted Privilege.</summary>
public class RolePrivilege
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public int PrivilegeId { get; set; }

    public Privilege Privilege { get; set; } = null!;
}
