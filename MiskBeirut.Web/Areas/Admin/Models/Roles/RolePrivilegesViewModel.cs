namespace MiskBeirut.Web.Areas.Admin.Models.Roles;

public class RolePrivilegesViewModel
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = "";

    /// <summary>The Admin role always has full access regardless of what's checked here — the checklist is shown read-only/disabled for it.</summary>
    public bool IsAdminRole { get; set; }

    public List<RoleSectionPrivilegesViewModel> Sections { get; set; } = [];
}

public class RoleSectionPrivilegesViewModel
{
    public int SectionPrivilegeId { get; set; }
    public string SectionName { get; set; } = "";

    /// <summary>Granting the section implies every page below it, even ones added later.</summary>
    public bool SectionGranted { get; set; }

    public List<RolePagePrivilegeViewModel> Pages { get; set; } = [];
}

public class RolePagePrivilegeViewModel
{
    public int PrivilegeId { get; set; }
    public string Name { get; set; } = "";
    public bool Granted { get; set; }
}
