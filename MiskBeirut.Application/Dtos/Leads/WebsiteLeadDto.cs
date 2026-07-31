namespace MiskBeirut.Application.Dtos.Leads;

public sealed record WebsiteLeadDto
{
    public int Id { get; init; }
    public string Name { get; init; } = null!;
    public string PhoneNumber { get; init; } = null!;
    public string Email { get; init; } = null!;
    public bool DiscountRedeemed { get; init; }
    public DateTime CreatedAt { get; init; }
}
