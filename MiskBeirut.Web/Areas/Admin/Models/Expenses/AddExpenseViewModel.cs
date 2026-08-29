using System.ComponentModel.DataAnnotations;

namespace MiskBeirut.Web.Areas.Admin.Models.Expenses;

/// <summary>
/// The Expenses Control Panel page's "Add Expense" form — always saved with no Daily Closing yet;
/// <see cref="Application.Managers.DailyClosingManager"/> attaches it automatically once one exists
/// for the same date.
/// </summary>
public class AddExpenseViewModel
{
    [Required]
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required, Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    public int ReceiverId { get; set; }

    public string? Note { get; set; }
}
