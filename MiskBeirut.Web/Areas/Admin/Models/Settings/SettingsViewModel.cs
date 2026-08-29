using MiskBeirut.Core.Entities;

namespace MiskBeirut.Web.Areas.Admin.Models.Settings;

/// <summary>The signed-in user's own account settings. A user can hold more than one role at
/// once (see <see cref="User"/>), so roles are a list here, not a single value.</summary>
public class SettingsViewModel
{
    public User User { get; set; } = null!;
    public IReadOnlyList<string> Roles { get; set; } = [];
}