namespace MiskBeirut.Application.Dtos.Customers;

public sealed record CustomerDto
{
    public int Id { get; init; }
    public string Name { get; init; } = null!;
    public string PhoneNumber { get; init; } = null!;
    public decimal Balance { get; init; }
    public DateTime CreatedAt { get; init; }
}
