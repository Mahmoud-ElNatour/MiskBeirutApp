using System.ComponentModel.DataAnnotations;

namespace MiskBeirut.Web.Areas.Admin.Models.Users;

public class CreateUserRequest
{
    [Required]
    public string Username { get; set; } = "";

    [EmailAddress]
    public string? Email { get; set; }

    [Required, MinLength(8)]
    public string Password { get; set; } = "";

    public List<string> SelectedRoles { get; set; } = [];
}
