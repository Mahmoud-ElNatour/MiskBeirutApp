using System.ComponentModel.DataAnnotations;
using MiskBeirut.Core.Enums;

namespace MiskBeirut.Web.Areas.Admin.Models.Customers;

public class AddCustomerLedgerEntryViewModel
{
    public int CustomerId { get; set; }

    [Required]
    public int DailyClosingId { get; set; }

    [Required]
    public CustomerLedgerType Type { get; set; }

    /// <summary>Always entered as a positive magnitude — the controller applies the sign convention (Credit negative, Cashback positive).</summary>
    [Required, Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    public string? Note { get; set; }
}
