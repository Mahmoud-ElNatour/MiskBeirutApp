using System.ComponentModel.DataAnnotations;

namespace MiskBeirut.Web.Areas.Admin.Models.Employees;

public class EmployeeFormViewModel
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = "";

    public string? PhoneNumber { get; set; }
    public string? Position { get; set; }

    [Range(0, 1_000_000)]
    public decimal BaseSalary { get; set; }

    public bool IsActive { get; set; } = true;
}
