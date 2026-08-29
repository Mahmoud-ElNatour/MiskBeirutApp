using System.ComponentModel.DataAnnotations;

namespace MiskBeirut.Web.Areas.Cms.Models;

public class LoginRequest
{
    [Required]
    public string Username { get; set; } = "";

    [Required]
    public string Password { get; set; } = "";
}
