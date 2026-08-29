using System.ComponentModel.DataAnnotations;

namespace MiskBeirut.Web.Areas.Admin.Models.Payroll;

public class PayrollFormViewModel
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = "";

    /// <summary>This month's own salary rate (see EmployeeWorking.BaseSalary) — pre-filled from the
    /// existing record, or the employee's current Base Salary for a month with no record yet.
    /// Editable here, independent of the employee's master record.</summary>
    [Range(0.01, 1_000_000, ErrorMessage = "Base Salary is required and must be greater than 0.")]
    public decimal BaseSalary { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public string? Status { get; set; }
    public int? WorkingDays { get; set; }
    public int? ActualWorkingDays { get; set; }
    public decimal? DeductionsTotal { get; set; }
    public decimal? AdvanceTotal { get; set; }
    public decimal? ActualSalary { get; set; }
    public decimal? Total { get; set; }
    public bool IsWorking { get; set; } = true;
    public string? Note { get; set; }
}
