namespace MiskBeirut.Core.Entities;

/// <summary>backoffice.expenses</summary>
public class Expense
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public int DailyClosingId { get; set; }
    public int ReceiverId { get; set; }

    public DailyClosing DailyClosing { get; set; } = null!;
    public Receiver Receiver { get; set; } = null!;
}
