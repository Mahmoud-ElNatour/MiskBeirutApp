namespace MiskBeirut.Application.Dtos.DailyClosings;

public sealed record CreateNonCashPaymentRequest
{
    public DateOnly Date { get; init; }
    public decimal Amount { get; init; }
    public string PaymentMethod { get; init; } = null!;
    public string? Note { get; init; }
    public int DailyClosingId { get; init; }
}
