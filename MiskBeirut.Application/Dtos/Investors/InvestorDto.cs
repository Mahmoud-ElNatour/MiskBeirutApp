namespace MiskBeirut.Application.Dtos.Investors;

public sealed record InvestorDto
{
    public int Id { get; init; }
    public string Name { get; init; } = null!;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}
