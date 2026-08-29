namespace MiskBeirut.Application.Dtos.Employees;

/// <summary>
/// Used for both creating and editing a month's working record — the natural key
/// (EmployeeId/Year/Month) decides which. No ActualSalary/Total here on purpose: PayrollManager
/// always computes those itself (EmployeeWorking.RecomputeSalary), never takes them from a caller.
/// </summary>
public sealed record SaveEmployeeWorkingRequest
{
    public int EmployeeId { get; init; }
    public int Year { get; init; }
    public int Month { get; init; }
    public string? Status { get; init; }
    public int? WorkingDays { get; init; }
    public int? ActualWorkingDays { get; init; }
    public decimal? DeductionsTotal { get; init; }
    public decimal? AdvanceTotal { get; init; }

    /// <summary>This month's salary rate override. Null keeps the record's existing BaseSalary
    /// unchanged (or, for a brand-new record, defaults to the employee's current Base Salary) —
    /// see EmployeeWorking.BaseSalary.</summary>
    public decimal? BaseSalary { get; init; }

    public bool IsWorking { get; init; } = true;
    public string? Note { get; init; }
}
