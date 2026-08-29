namespace MiskBeirut.Web.Areas.Admin.Models.Users;

public class UserListItem
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string? Email { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public string? AssignedEmployeeName { get; set; }
}
