namespace MiskBeirut.Application.Dtos.Investors;

public sealed record CreateInvestorRequest
{
    public string Name { get; init; } = null!;
}
