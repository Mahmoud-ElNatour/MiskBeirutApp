namespace MiskBeirut.Application.Dtos.DailyClosings;

public sealed record CreateExpenseRequest
{
    public DateOnly Date { get; init; }
    public decimal Amount { get; init; }
    public string? Note { get; init; }

    /// <summary>Null to add this expense with no Daily Closing yet — see <see cref="MiskBeirut.Core.Entities.Expense.DailyClosingId"/>.</summary>
    public int? DailyClosingId { get; init; }
    public int ReceiverId { get; init; }

    /// <summary>True for a standalone expense (not one of a Daily Closing's submitted line items) — see <see cref="MiskBeirut.Core.Entities.Expense.IsManualEntry"/>.</summary>
    public bool IsManualEntry { get; init; }
}
