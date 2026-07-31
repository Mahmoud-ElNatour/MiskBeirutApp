using MiskBeirut.Application.Dtos.DailyClosings;
using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Repositories;

namespace MiskBeirut.Application.Managers;

/// <summary>Line-item expenses attached to a daily closing.</summary>
public class ExpenseManager
{
    private readonly IExpenseRepository _expenses;

    public ExpenseManager(IExpenseRepository expenses)
    {
        _expenses = expenses;
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
            ReceiverId = request.ReceiverId
        }, cancellationToken);

        return ToDto(expense);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var expense = await _expenses.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Expense {id} was not found.");
        await _expenses.DeleteAsync(expense, cancellationToken);
    }

    private static ExpenseDto ToDto(Expense expense) => new()
    {
        Id = expense.Id,
        Date = expense.Date,
        Amount = expense.Amount,
        Note = expense.Note,
        DailyClosingId = expense.DailyClosingId,
        ReceiverId = expense.ReceiverId
    };
}
