namespace MiskBeirut.Application.Dtos.Careers;

/// <summary>
/// A vacancy as the Cms manages it: both languages side by side and inactive ones included, unlike
/// <see cref="VacancyDto"/>, which is one language resolved for a visitor.
/// </summary>
public sealed record VacancyAdminDto
{
    public int Id { get; init; }
    public string Slug { get; init; } = null!;
    public string Title { get; init; } = null!;
    public string Department { get; init; } = null!;
    public string Location { get; init; } = null!;
    public string EmploymentType { get; init; } = null!;
    public string? Description { get; init; }
    public string? Requirements { get; init; }

    public string? TitleAr { get; init; }
    public string? DepartmentAr { get; init; }
    public string? LocationAr { get; init; }
    public string? EmploymentTypeAr { get; init; }
    public string? DescriptionAr { get; init; }
    public string? RequirementsAr { get; init; }

    public DateTime? ApplicationDeadline { get; init; }
    public string Icon { get; init; } = null!;
    public bool IsActive { get; init; }
    public int DisplayOrder { get; init; }
    public DateTime CreatedAt { get; init; }

    /// <summary>How many applications have come in for this vacancy — shown before a delete, since deleting takes them with it.</summary>
    public int ApplicationCount { get; init; }

    /// <summary>True when a deadline is set and has already passed. Such a vacancy is hidden from the public page even while IsActive.</summary>
    public bool IsExpired => ApplicationDeadline.HasValue && ApplicationDeadline.Value.Date < DateTime.UtcNow.Date;
}
