using MiskBeirut.Application.Dtos.Employees;

namespace MiskBeirut.Web.Areas.Admin.Models.Payroll;

public class EmployeePayrollViewModel
{
    public EmployeeDto Employee { get; set; } = null!;
    public IReadOnlyList<EmployeeWorkingDto> Records { get; set; } = [];
}
