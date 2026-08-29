using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MiskBeirut.Core.Constants;

namespace MiskBeirut.Web.Areas.Admin.Models.Employees;

// Field names match the ported legacy JS payload (Areas/Admin/Views/Employees/_AddModal.cshtml / _EditModal.cshtml).
public class EmployeeApiRequest
{
    [Required]
    [StringLength(FieldLengths.Name)]
    public string Name { get; set; } = "";

    [JsonPropertyName("phone_number")]
    [Phone]
    [StringLength(FieldLengths.PhoneNumber)]
    public string? PhoneNumber { get; set; }

    [StringLength(FieldLengths.Position)]
    public string? Position { get; set; }

    [JsonPropertyName("base_salary")]
    public decimal BaseSalary { get; set; }

    [JsonPropertyName("working_days")]
    public decimal WorkingDays { get; set; }

    public int Year { get; set; }
    public int Month { get; set; }
}
