using MiskBeirut.Application.Dtos.DailyClosings;
using MiskBeirut.Application.Dtos.Employees;

namespace MiskBeirut.Web.Areas.Admin.Models.Employees;

public class EmployeeDetailsViewModel
{
    public EmployeeDto Employee { get; set; } = null!;
    public IReadOnlyList<EmployeeLedgerEntryDto> Ledger { get; set; } = [];
    public IReadOnlyList<EmployeeWorkingDto> WorkingRecords { get; set; } = [];
    public IReadOnlyList<DailyClosingDto> RecentClosings { get; set; } = [];
    public AddEmployeeLedgerEntryViewModel NewEntry { get; set; } = new();
}
