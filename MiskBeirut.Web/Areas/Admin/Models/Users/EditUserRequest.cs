using System.ComponentModel.DataAnnotations;

namespace MiskBeirut.Web.Areas.Admin.Models.Users;

public class EditUserRequest
{
    public int Id { get; set; }

    public string Username { get; set; } = "";

    [EmailAddress]
    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public List<string> SelectedRoles { get; set; } = [];
}
