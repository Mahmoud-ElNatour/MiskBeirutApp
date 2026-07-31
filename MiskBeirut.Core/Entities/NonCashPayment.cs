namespace MiskBeirut.Core.Entities;

/// <summary>backoffice.non_cash_payments</summary>
public class NonCashPayment
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = null!;
    public string? Note { get; set; }
    public int DailyClosingId { get; set; }

    public DailyClosing DailyClosing { get; set; } = null!;
}
