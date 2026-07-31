namespace MiskBeirut.Application.Dtos.Employees;

public sealed record CreateEmployeeRequest
{
    public string Name { get; init; } = null!;
    public string? PhoneNumber { get; init; }
    public string? Position { get; init; }
    public decimal BaseSalary { get; init; }
}
