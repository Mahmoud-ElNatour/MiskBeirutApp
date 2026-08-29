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

    /// <summary>
    /// Null for an entry that isn't tied to any specific day's cash register — e.g. a carried-over
    /// shortfall applied automatically at the start of a new month (see
    /// EmployeeManager.EnsureCurrentMonthWorkingRecordsAsync). A manually-entered Advance/Deduct from
    /// the Employee Details page still always names one, via its required dropdown.
    /// </summary>
    public int? DailyClosingId { get; set; }

    public Employee Employee { get; set; } = null!;
    public DailyClosing? DailyClosing { get; set; }
}
