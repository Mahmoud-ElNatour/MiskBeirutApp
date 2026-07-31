namespace MiskBeirut.Application.Dtos.Customers;

public sealed record CreateCustomerRequest
{
    public string Name { get; init; } = null!;
    public string PhoneNumber { get; init; } = null!;
}
