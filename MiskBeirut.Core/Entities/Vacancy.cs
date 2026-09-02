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

    /// <summary>What the role involves, shown when a visitor expands the vacancy on the Careers page. Optional.</summary>
    public string? Description { get; set; }

    /// <summary>What the candidate needs, shown beside <see cref="Description"/>. One requirement per line — the Careers page renders the lines as a bulleted list. Optional.</summary>
    public string? Requirements { get; set; }

    /// <summary>
    /// Arabic copy shown when the visitor is browsing in Arabic. Each falls back to its English
    /// counterpart when unset, so a vacancy added without a translation still renders.
    /// </summary>
    public string? TitleAr { get; set; }
    public string? DepartmentAr { get; set; }
    public string? LocationAr { get; set; }
    public string? EmploymentTypeAr { get; set; }
    public string? DescriptionAr { get; set; }
    public string? RequirementsAr { get; set; }

    /// <summary>
    /// Last day applications are accepted (date only — the whole day counts). Null means the vacancy
    /// stays open until someone deactivates it. Once the deadline has passed the vacancy stops
    /// appearing on the public Careers page, without an editor having to remember to switch it off.
    /// </summary>
    public DateTime? ApplicationDeadline { get; set; }

    public string Icon { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
}
