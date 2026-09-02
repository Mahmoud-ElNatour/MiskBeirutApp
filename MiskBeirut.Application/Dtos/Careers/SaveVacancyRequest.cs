namespace MiskBeirut.Application.Dtos.Careers;

/// <summary>Create/update payload for a vacancy edited in the Cms. Id is 0 for a new one.</summary>
public sealed record SaveVacancyRequest
{
    public int Id { get; init; }

    /// <summary>Left blank on create, the manager derives one from the title.</summary>
    public string? Slug { get; init; }

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
    public string? Icon { get; init; }
    public bool IsActive { get; init; }
    public int DisplayOrder { get; init; }
}
