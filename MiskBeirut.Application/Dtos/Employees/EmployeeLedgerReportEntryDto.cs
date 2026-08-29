using MiskBeirut.Core.Enums;

namespace MiskBeirut.Application.Dtos.Employees;

/// <summary>A ledger entry plus the owning employee's name, for the cross-employee Deductions &amp; Advances control-panel report.</summary>
public sealed record EmployeeLedgerReportEntryDto
{
    public int Id { get; init; }
    public DateOnly Date { get; init; }
    public decimal Amount { get; init; }
    public EmployeeLedgerType Type { get; init; }
    public string? Note { get; init; }
    public int EmployeeId { get; init; }
    public string EmployeeName { get; init; } = null!;
}
