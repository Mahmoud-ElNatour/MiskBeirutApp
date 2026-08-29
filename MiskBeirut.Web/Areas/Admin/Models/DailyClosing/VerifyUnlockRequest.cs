namespace MiskBeirut.Web.Areas.Admin.Models.DailyClosing;

/// <summary>Posted by the Create page's date-unlock modal: a Supervisor/Admin co-signs to let the
/// currently logged-in user (who may only be an Employee) edit the locked Date field.</summary>
public class VerifyUnlockRequest
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}
