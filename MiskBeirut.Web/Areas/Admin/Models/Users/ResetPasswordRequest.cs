using System.ComponentModel.DataAnnotations;

namespace MiskBeirut.Web.Areas.Admin.Models.Users;

public class ResetPasswordRequest
{
    public int Id { get; set; }

    public string Username { get; set; } = "";

    [Required, MinLength(8)]
    public string NewPassword { get; set; } = "";
}
