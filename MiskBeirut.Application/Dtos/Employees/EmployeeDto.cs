namespace MiskBeirut.Application.Dtos.Employees;

public sealed record EmployeeDto
{
    public int Id { get; init; }
    public string Name { get; init; } = null!;
    public string? PhoneNumber { get; init; }
    public string? Position { get; init; }
    public decimal BaseSalary { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public int? UserId { get; init; }
}
