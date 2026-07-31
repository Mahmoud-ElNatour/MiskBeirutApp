namespace MiskBeirut.Application.Dtos.DailyClosings;

public sealed record ExpenseDto
{
    public int Id { get; init; }
    public DateOnly Date { get; init; }
    public decimal Amount { get; init; }
    public string? Note { get; init; }
    public int DailyClosingId { get; init; }
    public int ReceiverId { get; init; }
}
