namespace MiskBeirut.Web.Areas.Admin.Models.Payroll;

public class PayrollFormViewModel
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = "";
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
