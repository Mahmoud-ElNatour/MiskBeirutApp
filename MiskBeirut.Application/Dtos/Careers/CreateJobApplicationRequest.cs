namespace MiskBeirut.Application.Dtos.Careers;

public sealed record CreateJobApplicationRequest
{
    public string Name { get; init; } = null!;
    public string PhoneNumber { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string? Address { get; init; }
    public int VacancyId { get; init; }
}
