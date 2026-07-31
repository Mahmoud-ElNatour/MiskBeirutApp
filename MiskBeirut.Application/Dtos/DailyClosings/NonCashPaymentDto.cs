namespace MiskBeirut.Application.Dtos.DailyClosings;

public sealed record NonCashPaymentDto
{
    public int Id { get; init; }
    public DateOnly Date { get; init; }
    public decimal Amount { get; init; }
    public string PaymentMethod { get; init; } = null!;
    public string? Note { get; init; }
    public int DailyClosingId { get; init; }
}
