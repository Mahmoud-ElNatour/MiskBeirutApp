using MiskBeirut.Core.Enums;

namespace MiskBeirut.Application.Dtos.Employees;

public sealed record EmployeeLedgerEntryDto
{
    public int Id { get; init; }
    public DateOnly Date { get; init; }
    public decimal Amount { get; init; }
    public EmployeeLedgerType Type { get; init; }
    public string? Note { get; init; }
    public int EmployeeId { get; init; }
    public int DailyClosingId { get; init; }
}
