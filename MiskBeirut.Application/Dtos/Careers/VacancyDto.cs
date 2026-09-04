namespace MiskBeirut.Application.Dtos.Careers;

/// <summary>One open position as the public Careers page sees it — already resolved to the visitor's language.</summary>
public sealed record VacancyDto
{
    public int Id { get; init; }
    public string Slug { get; init; } = null!;
    public string Title { get; init; } = null!;
    public string Department { get; init; } = null!;
    public string Location { get; init; } = null!;
    public string EmploymentType { get; init; } = null!;
    public string? Description { get; init; }

    /// <summary>Requirements as entered, one per line. <see cref="RequirementLines"/> is what the view renders.</summary>
    public string? Requirements { get; init; }

    public DateTime? ApplicationDeadline { get; init; }
    public string Icon { get; init; } = null!;

    /// <summary>When the vacancy was posted. JobPosting structured data has to state a datePosted, and a listing without one is dropped from Google for Jobs.</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>True when there is anything to show in the expandable details section.</summary>
    public bool HasDetails => !string.IsNullOrWhiteSpace(Description) || RequirementLines.Count > 0;

    /// <summary>The requirements split into list items — blank lines and stray bullet characters dropped.</summary>
    public IReadOnlyList<string> RequirementLines => string.IsNullOrWhiteSpace(Requirements)
        ? []
        : Requirements.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => line.TrimStart('-', '*', '•', ' '))
            .Where(line => line.Length > 0)
            .ToList();
}
