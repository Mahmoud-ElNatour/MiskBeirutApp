using System.ComponentModel.DataAnnotations;
using MiskBeirut.Core.Enums;

namespace MiskBeirut.Web.Areas.Admin.Models.Employees;

public class AddEmployeeLedgerEntryViewModel
{
    public int EmployeeId { get; set; }

    [Required]
    public int DailyClosingId { get; set; }

    [Required]
    public EmployeeLedgerType Type { get; set; }

    /// <summary>Entered as a positive magnitude — both Advance and Deduct are stored negative; the controller applies the sign.</summary>
    [Required, Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    public string? Note { get; set; }
}
