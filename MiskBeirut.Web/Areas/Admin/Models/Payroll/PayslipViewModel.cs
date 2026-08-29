using MiskBeirut.Application.Dtos.Employees;

namespace MiskBeirut.Web.Areas.Admin.Models.Payroll;

/// <summary>One employee's payslip for one month — the printable breakdown.</summary>
public class PayslipViewModel
{
    public EmployeeDto Employee { get; set; } = null!;
    public EmployeeWorkingDto Record { get; set; } = null!;
    public IReadOnlyList<EmployeeLedgerEntryDto> Advances { get; set; } = [];
    public IReadOnlyList<EmployeeLedgerEntryDto> Deductions { get; set; } = [];
}