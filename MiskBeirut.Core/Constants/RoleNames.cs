namespace MiskBeirut.Core.Constants;

/// <summary>
/// Identity role names. A user can belong to more than one of these (see backoffice.user_roles).
/// </summary>
public static class RoleNames
{
    /// <summary>Full access to everything in the system.</summary>
    public const string Admin = "Admin";

    /// <summary>Cms area only.</summary>
    public const string Content = "Content";

    /// <summary>Payroll read; Daily Closing read/write; Customer (backoffice) read/write.</summary>
    public const string Supervisor = "Supervisor";

    /// <summary>Daily Closing write only — cannot read prior days' closings.</summary>
    public const string Employee = "Employee";

    public static readonly IReadOnlyList<string> All = [Admin, Content, Supervisor, Employee];
}
