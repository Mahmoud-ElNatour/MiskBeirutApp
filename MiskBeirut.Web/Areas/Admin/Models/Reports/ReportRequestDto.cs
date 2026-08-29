namespace MiskBeirut.Web.Areas.Admin.Models.Reports;

/// <summary>Body of the Reports page's Sales/Payroll/Expenses generate requests — a plain month+year filter.</summary>
public class ReportRequestDto
{
    public int Month { get; set; }
    public int Year { get; set; }
}
