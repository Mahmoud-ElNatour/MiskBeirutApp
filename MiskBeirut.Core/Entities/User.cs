using Microsoft.AspNetCore.Identity;

namespace MiskBeirut.Core.Entities;

/// <summary>
/// backoffice.users. Role membership is many-to-many (see backoffice.user_roles / <see cref="MiskBeirut.Core.Constants.RoleNames"/>) —
/// a user can hold more than one role at once, so there is no single "Role" column here.
/// </summary>
public class User : IdentityUser<int>
{
    /// <summary>Back-compat alias for <see cref="IdentityUser{TKey}.UserName"/> for code written before the Identity conversion.</summary>
    public string Username
    {
        get => UserName ?? "";
        set => UserName = value;
    }

    public DateTime CreatedAt { get; set; }

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
