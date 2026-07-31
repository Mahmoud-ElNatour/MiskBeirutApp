using MiskBeirut.Core.Enums;

namespace MiskBeirut.Core.Entities;

/// <summary>
/// backoffice.employee_ledger — advances and deductions, always stored as negative amounts.
/// </summary>
public class EmployeeLedger
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public decimal Amount { get; set; }
    public EmployeeLedgerType Type { get; set; }
    public string? Note { get; set; }
    public int EmployeeId { get; set; }
    public int DailyClosingId { get; set; }

    public Employee Employee { get; set; } = null!;
    public DailyClosing DailyClosing { get; set; } = null!;
}
