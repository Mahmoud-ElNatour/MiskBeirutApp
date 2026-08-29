using MiskBeirut.Application.Dtos.DailyClosings;
using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Repositories;

namespace MiskBeirut.Application.Managers;

/// <summary>Line-item expenses attached to a daily closing.</summary>
public class ExpenseManager
{
    private readonly IExpenseRepository _expenses;
    private readonly IDailyClosingRepository _dailyClosings;

    // Depends on IDailyClosingRepository directly rather than DailyClosingManager — the latter
    // already depends on ExpenseManager (it posts each closing's expenses through it), so going the
    // other way would be a circular dependency. Same pattern as CustomerManager/EmployeeManager.
    public ExpenseManager(IExpenseRepository expenses, IDailyClosingRepository dailyClosings)
    {
        _expenses = expenses;
        _dailyClosings = dailyClosings;
    }

    public async Task<IReadOnlyList<ExpenseDto>> GetByDailyClosingAsync(int dailyClosingId, CancellationToken cancellationToken = default)
    {
        var expenses = await _expenses.GetByDailyClosingAsync(dailyClosingId, cancellationToken);
        return expenses.Select(ToDto).ToList();
    }

    public async Task<ExpenseDto> AddAsync(CreateExpenseRequest request, CancellationToken cancellationToken = default)
    {
        var expense = await _expenses.AddAsync(new Expense
        {
            Date = request.Date,
            Amount = request.Amount,
            Note = request.Note,
            DailyClosingId = request.DailyClosingId,
            ReceiverId = request.ReceiverId,
            IsManualEntry = request.IsManualEntry
        }, cancellationToken);

        if (request.DailyClosingId is int closingId)
            await NudgeClosingActualCashAsync(closingId, -request.Amount, cancellationToken);

        return ToDto(expense);
    }

    /// <summary>
    /// ActualCash is computed once (see DailyClosingManager.ApplyComputedTotals) when a closing is
    /// created/edited via the New/Edit Close forms and stored as a plain column rather than derived
    /// live. An expense added to or removed from an existing closing afterward — from the Daily
    /// Closing Details page's per-line Add/Delete — has to nudge that same stored total by
    /// <paramref name="delta"/> (an expense subtracts from cash, so Add passes a negative delta and
    /// Delete passes the reversal), or the Sales Dashboard keeps showing stale numbers. No-ops if
    /// the closing has no computed ActualCash yet (mid-way through DailyClosingManager building a
    /// brand new one).
    /// </summary>
    private async Task NudgeClosingActualCashAsync(int dailyClosingId, decimal delta, CancellationToken cancellationToken)
    {
        var closing = await _dailyClosings.GetByIdAsync(dailyClosingId, cancellationToken);
        if (closing?.ActualCash is null)
            return;

        closing.ActualCash += delta;
        await _dailyClosings.UpdateAsync(closing, cancellationToken);
    }

    /// <summary>
    /// Attaches every manual expense (see <c>Expense.IsManualEntry</c>) dated <paramref name="date"/>
    /// that has no Daily Closing yet to <paramref name="dailyClosingId"/> — called by
    /// <see cref="DailyClosingManager"/> whenever a closing for that date is created or edited, so a
    /// manual expense entered ahead of the closing still folds into its ActualCash total. Returns the
    /// sum attached, for the caller's own totals recompute (no per-entry nudge here).
    /// </summary>
    public async Task<decimal> AttachManualEntriesAsync(DateOnly date, int dailyClosingId, CancellationToken cancellationToken = default)
    {
        var unattached = await _expenses.GetUnattachedByDateAsync(date, cancellationToken);
        decimal total = 0;
        foreach (var expense in unattached)
        {
            expense.DailyClosingId = dailyClosingId;
            await _expenses.UpdateAsync(expense, cancellationToken);
            total += expense.Amount;
        }
        return total;
    }

    private static readonly DateOnly EarliestPossibleDate = new(2000, 1, 1);

    public async Task<IReadOnlyList<ExpenseDto>> GetByReceiverAsync(int receiverId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var expenses = await _expenses.GetByDateRangeAsync(from, to, cancellationToken);
        return expenses.Where(e => e.ReceiverId == receiverId).Select(ToDto).ToList();
    }

    public async Task<decimal> GetTotalPaidByReceiverAsync(int receiverId, CancellationToken cancellationToken = default)
    {
        var expenses = await _expenses.GetByDateRangeAsync(EarliestPossibleDate, DateOnly.FromDateTime(DateTime.UtcNow), cancellationToken);
        return expenses.Where(e => e.ReceiverId == receiverId).Sum(e => e.Amount);
    }

    /// <summary>Cross-receiver report for the Control Panel's Expenses page.</summary>
    public async Task<IReadOnlyList<ExpenseReportEntryDto>> GetReportAsync(int? month, int? year, int? receiverId, CancellationToken cancellationToken = default)
    {
        var expenses = await _expenses.GetReportAsync(month, year, receiverId, cancellationToken);
        return expenses.Select(e => new ExpenseReportEntryDto
        {
            Id = e.Id,
            Date = e.Date,
            Amount = e.Amount,
            Note = e.Note,
            ReceiverId = e.ReceiverId,
            ReceiverName = e.Receiver.Name
        }).ToList();
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var expense = await _expenses.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Expense {id} was not found.");
        await _expenses.DeleteAsync(expense, cancellationToken);
        if (expense.DailyClosingId is int closingId)
            await NudgeClosingActualCashAsync(closingId, expense.Amount, cancellationToken);
    }

    private static ExpenseDto ToDto(Expense expense) => new()
    {
        Id = expense.Id,
        Date = expense.Date,
        Amount = expense.Amount,
        Note = expense.Note,
        DailyClosingId = expense.DailyClosingId,
        ReceiverId = expense.ReceiverId,
        IsManualEntry = expense.IsManualEntry
    };
}
