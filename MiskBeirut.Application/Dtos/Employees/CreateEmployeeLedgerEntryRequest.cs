using MiskBeirut.Core.Enums;

namespace MiskBeirut.Application.Dtos.Employees;

/// <summary>
/// Both Advance and Deduct entries are stored as negative amounts —
/// enforced by <c>EmployeeManager</c> before the row is written.
/// </summary>
public sealed record CreateEmployeeLedgerEntryRequest
{
    public DateOnly Date { get; init; }
    public decimal Amount { get; init; }
    public EmployeeLedgerType Type { get; init; }
    public string? Note { get; init; }
    public int EmployeeId { get; init; }
    public int DailyClosingId { get; init; }
}
