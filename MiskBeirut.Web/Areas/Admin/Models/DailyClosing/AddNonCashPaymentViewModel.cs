using System.ComponentModel.DataAnnotations;

namespace MiskBeirut.Web.Areas.Admin.Models.DailyClosing;

public class AddNonCashPaymentViewModel
{
    public int DailyClosingId { get; set; }

    [Required]
    public decimal Amount { get; set; }

    [Required]
    public string PaymentMethod { get; set; } = "";

    public string? Note { get; set; }
}
