namespace MiskBeirut.Application.Dtos.DailyClosings;

/// <summary>An expense plus its receiver's name, for the cross-receiver Expenses control-panel report.</summary>
public sealed record ExpenseReportEntryDto
{
    public int Id { get; init; }
    public DateOnly Date { get; init; }
    public decimal Amount { get; init; }
    public string? Note { get; init; }
    public int ReceiverId { get; init; }
    public string ReceiverName { get; init; } = null!;
}
