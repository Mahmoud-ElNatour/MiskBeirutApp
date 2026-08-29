namespace MiskBeirut.Core.Entities;

/// <summary>backoffice.employee_working</summary>
public class EmployeeWorking
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public string? Status { get; set; }
    public int? WorkingDays { get; set; }
    public int? ActualWorkingDays { get; set; }
    public decimal? DeductionsTotal { get; set; }
    public decimal? AdvanceTotal { get; set; }

    /// <summary>
    /// This month's own salary rate — a snapshot, not a live read of Employee.BaseSalary. Defaults
    /// from the employee's current Base Salary the first time a record for this month is created
    /// (see PayrollManager.SaveAsync / EmployeeRepository.AddLedgerEntryAsync), then stays put even
    /// if the employee's salary changes later — a raise today shouldn't retroactively change what
    /// July was computed with. Editable per month via the Payroll Edit form for the rare case a
    /// month's rate genuinely needs correcting (a raise effective mid-month, a one-off adjustment).
    /// </summary>
    public decimal BaseSalary { get; set; }

    public decimal? ActualSalary { get; set; }
    public decimal? Total { get; set; }
    public DateOnly? StartedAt { get; set; }
    public DateOnly? EndedAt { get; set; }
    public bool IsWorking { get; set; } = true;
    public string? Note { get; set; }

    public Employee Employee { get; set; } = null!;

    /// <summary>
    /// Recomputes ActualSalary and Total from this record's own BaseSalary, ActualWorkingDays,
    /// DeductionsTotal and AdvanceTotal:
    /// ActualSalary = (BaseSalary ÷ 30) × ActualWorkingDays − Deductions − Advance.
    /// Total mirrors ActualSalary except floored at 0 — a month where advances/deductions
    /// outweigh what was earned owes nothing further, it isn't a negative payout.
    /// Called from both PayrollManager.SaveAsync (editing a month's record directly) and
    /// EmployeeRepository's ledger-entry add/delete (an Advance/Deduct changes the totals this
    /// formula depends on, so it has to be kept in sync there too, not just here).
    /// </summary>
    public void RecomputeSalary()
    {
        var dailyRate = BaseSalary / 30m;
        var actualSalary = dailyRate * (ActualWorkingDays ?? 0) - (DeductionsTotal ?? 0) - (AdvanceTotal ?? 0);
        ActualSalary = actualSalary;
        Total = actualSalary < 0 ? 0 : actualSalary;
    }
}
