using System.ComponentModel.DataAnnotations;
using MiskBeirut.Core.Constants;

namespace MiskBeirut.Web.Areas.Admin.Models.Employees;

public class EmployeeFormViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(FieldLengths.Name)]
    public string Name { get; set; } = "";

    [Phone]
    [StringLength(FieldLengths.PhoneNumber)]
    public string? PhoneNumber { get; set; }

    [StringLength(FieldLengths.Position)]
    public string? Position { get; set; }

    [Range(0.01, 1_000_000, ErrorMessage = "Base Salary is required and must be greater than 0.")]
    public decimal BaseSalary { get; set; }

    public bool IsActive { get; set; } = true;
}
