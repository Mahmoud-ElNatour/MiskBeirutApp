using System.ComponentModel.DataAnnotations;

namespace MiskBeirut.Web.Areas.Admin.Models.DailyClosing;

public class AddExpenseViewModel
{
    public int DailyClosingId { get; set; }

    [Required]
    public decimal Amount { get; set; }

    [Required]
    public int ReceiverId { get; set; }

    public string? Note { get; set; }
}
