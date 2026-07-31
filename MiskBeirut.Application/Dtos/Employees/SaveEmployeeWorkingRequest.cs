namespace MiskBeirut.Application.Dtos.Employees;

/// <summary>Used for both creating and editing a month's working record — the natural key (EmployeeId/Year/Month) decides which.</summary>
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
    public decimal? ActualSalary { get; init; }
    public decimal? Total { get; init; }
    public bool IsWorking { get; init; } = true;
    public string? Note { get; init; }
}
