namespace MiskBeirut.Core.Entities;

/// <summary>An open position shown on the public Careers page.</summary>
public class Vacancy
{
    public int Id { get; set; }
    public string Slug { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Department { get; set; } = null!;
    public string Location { get; set; } = null!;
    public string EmploymentType { get; set; } = null!;

    /// <summary>
    /// Arabic copy shown when the visitor is browsing in Arabic. Each falls back to its English
    /// counterpart when unset, so a vacancy added without a translation still renders.
    /// </summary>
    public string? TitleAr { get; set; }
    public string? DepartmentAr { get; set; }
    public string? LocationAr { get; set; }
    public string? EmploymentTypeAr { get; set; }

    public string Icon { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
}
