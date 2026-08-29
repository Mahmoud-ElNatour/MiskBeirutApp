using MiskBeirut.Application.Dtos.Employees;

namespace MiskBeirut.Web.Areas.Admin.Models.Payroll;

/// <summary>One row of the Payroll Index table — an active employee paired with their working
/// record for the selected month, if one has been entered yet.</summary>
public class PayrollIndexRowViewModel
{
    public EmployeeDto Employee { get; set; } = null!;
    public EmployeeWorkingDto? Record { get; set; }
}