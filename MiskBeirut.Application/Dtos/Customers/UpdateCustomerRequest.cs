namespace MiskBeirut.Application.Dtos.Customers;

public sealed record UpdateCustomerRequest
{
    public string Name { get; init; } = null!;
    public string PhoneNumber { get; init; } = null!;
}
