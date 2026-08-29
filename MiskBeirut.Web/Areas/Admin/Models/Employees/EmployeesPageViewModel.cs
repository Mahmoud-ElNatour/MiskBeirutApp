namespace MiskBeirut.Web.Areas.Admin.Models.Employees;

public class EmployeesPageViewModel
{
    public List<EmployeeWorkingRecordViewModel> Records { get; set; } = new();
    public int CurrentYear { get; set; }
    public int CurrentMonth { get; set; }
    public string ViewType { get; set; } = "working";
    public string Search { get; set; } = "";
    public string MonthName { get; set; } = "";
}

public class EmployeeWorkingRecordViewModel
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public bool IsWorking { get; set; }
    public decimal WorkingDays { get; set; }
    public EmployeeShortViewModel Employee { get; set; } = new();
}

public class EmployeeShortViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? PhoneNumber { get; set; }
    public string? Position { get; set; }
    public decimal BaseSalary { get; set; }
}
