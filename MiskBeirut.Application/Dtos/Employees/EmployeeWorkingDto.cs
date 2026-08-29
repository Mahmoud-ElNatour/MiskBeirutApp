namespace MiskBeirut.Application.Dtos.Employees;

public sealed record EmployeeWorkingDto
{
    public int Id { get; init; }
    public int EmployeeId { get; init; }
    public int Year { get; init; }
    public int Month { get; init; }
    public string? Status { get; init; }
    public int? WorkingDays { get; init; }
    public int? ActualWorkingDays { get; init; }
    public decimal? DeductionsTotal { get; init; }
    public decimal? AdvanceTotal { get; init; }
    public decimal BaseSalary { get; init; }
    public decimal? ActualSalary { get; init; }
    public decimal? Total { get; init; }
    public DateOnly? StartedAt { get; init; }
    public DateOnly? EndedAt { get; init; }
    public bool IsWorking { get; init; }
    public string? Note { get; init; }
}
