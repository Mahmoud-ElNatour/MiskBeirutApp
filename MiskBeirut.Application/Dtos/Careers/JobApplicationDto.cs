namespace MiskBeirut.Application.Dtos.Careers;

public sealed record JobApplicationDto
{
    public int Id { get; init; }
    public string Name { get; init; } = null!;
    public string PhoneNumber { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string? Address { get; init; }
    public string CvUrl { get; init; } = null!;
    public int VacancyId { get; init; }
    public string? VacancyTitle { get; init; }
    public DateTime CreatedAt { get; init; }
    public bool DecisionTaken { get; init; }
}
