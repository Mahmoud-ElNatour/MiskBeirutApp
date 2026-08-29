using System.ComponentModel.DataAnnotations;
using MiskBeirut.Core.Enums;

namespace MiskBeirut.Web.Areas.Admin.Models.Investors;

public class AddInvestorTransactionViewModel
{
    public int InvestorId { get; set; }

    [Required]
    public int DailyClosingId { get; set; }

    [Required]
    public InvestorTransactionType TransactionType { get; set; }

    /// <summary>Required only when TransactionType is Expense — validated in the controller, same as ReceiverId's DB requirement.</summary>
    public int? ReceiverId { get; set; }

    [Required, Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    public string? Note { get; set; }
}
