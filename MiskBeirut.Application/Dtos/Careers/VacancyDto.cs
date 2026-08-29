namespace MiskBeirut.Application.Dtos.Careers;

public sealed record VacancyDto
{
    public int Id { get; init; }
    public string Slug { get; init; } = null!;
    public string Title { get; init; } = null!;
    public string Department { get; init; } = null!;
    public string Location { get; init; } = null!;
    public string EmploymentType { get; init; } = null!;
    public string Icon { get; init; } = null!;
}
