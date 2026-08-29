namespace MiskBeirut.Web.Areas.Admin.Models.Settings;

/// <summary>Posted by both forms on the Settings page — profile fields are always sent;
/// password fields are only populated when the user is changing their password.</summary>
public class UpdateSettingsRequest
{
    public string Username { get; set; } = "";
    public string? Email { get; set; }

    public string? CurrentPassword { get; set; }
    public string? NewPassword { get; set; }
    public string? ConfirmPassword { get; set; }
}