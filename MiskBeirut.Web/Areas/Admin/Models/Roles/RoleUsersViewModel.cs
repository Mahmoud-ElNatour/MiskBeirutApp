namespace MiskBeirut.Web.Areas.Admin.Models.Roles;

/// <summary>The Role Manager's counterpart to Users/Edit's per-user role checklist — same
/// membership, edited from the role's side instead of the user's.</summary>
public class RoleUsersViewModel
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = "";

    /// <summary>Removing the last Admin-role user is blocked server-side too — this just lets the
    /// view explain why, same as Users/Edit's equivalent guard.</summary>
    public bool IsAdminRole { get; set; }

    public List<RoleUserItemViewModel> Users { get; set; } = [];
}

public class RoleUserItemViewModel
{
    public int UserId { get; set; }
    public string Username { get; set; } = "";
    public string? Email { get; set; }
    public bool Assigned { get; set; }
}
