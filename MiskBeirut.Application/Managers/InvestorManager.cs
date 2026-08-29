using MiskBeirut.Application.Dtos.Investors;
using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Enums;
using MiskBeirut.Core.Repositories;

namespace MiskBeirut.Application.Managers;

/// <summary>Investors and their withdrawal/expense transactions.</summary>
public class InvestorManager
{
    private readonly IInvestorRepository _investors;
    private readonly IDailyClosingRepository _dailyClosings;

    // Depends on IDailyClosingRepository directly rather than DailyClosingManager — the latter
    // already depends on InvestorManager (it posts each closing's investor expenses through it), so
    // going the other way would be a circular dependency.
    public InvestorManager(IInvestorRepository investors, IDailyClosingRepository dailyClosings)
    {
        _investors = investors;
        _dailyClosings = dailyClosings;
    }

    public async Task<IReadOnlyList<InvestorDto>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var investors = await _investors.GetActiveAsync(cancellationToken);
        return investors.Select(ToDto).ToList();
    }

    /// <summary>Deactivated investors — the "Inactive Investors" list on the Investors page, so Deactivate always has a way back.</summary>
    public async Task<IReadOnlyList<InvestorDto>> GetInactiveAsync(CancellationToken cancellationToken = default)
    {
        var investors = await _investors.GetAllAsync(cancellationToken);
        return investors.Where(i => !i.IsActive).Select(ToDto).ToList();
    }

    /// <summary>
    /// "Delete investor" — a soft delete: hides them from the active list and every picker (Daily
    /// Close, new transactions) without touching their transaction history, which Reports and their
    /// own Details page still need. Investor has no hard-delete path — most investors have Expense/
    /// Withdrawal transactions, which the FK on InvestorTransaction (Restrict) would either block or,
    /// if ever relaxed, silently orphan.
    /// </summary>
    public async Task DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var investor = await _investors.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Investor {id} was not found.");

        investor.IsActive = false;
        await _investors.UpdateAsync(investor, cancellationToken);
    }

    public async Task ReactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var investor = await _investors.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Investor {id} was not found.");

        investor.IsActive = true;
        await _investors.UpdateAsync(investor, cancellationToken);
    }

    public async Task<InvestorDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var investor = await _investors.GetByIdAsync(id, cancellationToken);
        return investor is null ? null : ToDto(investor);
    }

    public async Task<InvestorDto> CreateAsync(CreateInvestorRequest request, CancellationToken cancellationToken = default)
    {
        var investor = await _investors.AddAsync(new Investor
        {
            Name = request.Name,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        return ToDto(investor);
    }

    /// <summary>Cross-investor Expense transactions for a period — the Reports page's "investor expenses" breakdown (money paid out of investor capital, as opposed to the general Expenses book).</summary>
    public async Task<IReadOnlyList<InvestorTransactionDto>> GetExpenseReportAsync(int? month, int? year, CancellationToken cancellationToken = default)
    {
        var transactions = await _investors.GetTransactionsByTypeAsync(InvestorTransactionType.Expense, month, year, cancellationToken);
        return transactions.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<InvestorTransactionDto>> GetTransactionsAsync(int investorId, CancellationToken cancellationToken = default)
    {
        var transactions = await _investors.GetTransactionsAsync(investorId, cancellationToken);
        return transactions.Select(ToDto).ToList();
    }

    /// <summary>
    /// Adds a transaction. Expense transactions must name a receiver; Withdrawal
    /// transactions do not. Validated here so the failure is a clear error rather
    /// than a database check-constraint exception.
    /// </summary>
    public async Task<InvestorTransactionDto> AddTransactionAsync(CreateInvestorTransactionRequest request, CancellationToken cancellationToken = default)
    {
        if (request.TransactionType == InvestorTransactionType.Expense && request.ReceiverId is null)
            throw new InvalidOperationException("Expense transactions require a ReceiverId.");

        _ = await _investors.GetByIdAsync(request.InvestorId, cancellationToken)
            ?? throw new InvalidOperationException($"Investor {request.InvestorId} was not found.");

        var transaction = await _investors.AddTransactionAsync(new InvestorTransaction
        {
            Date = request.Date,
            Amount = request.Amount,
            TransactionType = request.TransactionType,
            Note = request.Note,
            DailyClosingId = request.DailyClosingId,
            InvestorId = request.InvestorId,
            ReceiverId = request.ReceiverId
        }, cancellationToken);

        // Only Expense transactions come out of today's cash drawer (see ApplyComputedTotals) —
        // Withdrawal transactions are investor capital movements the closing's totals never include,
        // so they must not touch ActualCash.
        if (request.TransactionType == InvestorTransactionType.Expense)
            await NudgeClosingActualCashAsync(request.DailyClosingId, -request.Amount, cancellationToken);

        return ToDto(transaction);
    }

    /// <summary>
    /// ActualCash is computed once (see DailyClosingManager.ApplyComputedTotals) when a closing is
    /// created/edited via the New/Edit Close forms and stored as a plain column rather than derived
    /// live. An investor Expense added to an existing closing afterward has to nudge that same
    /// stored total by <paramref name="delta"/> or the Sales Dashboard keeps showing stale numbers.
    /// No-ops if the closing has no computed ActualCash yet (mid-way through DailyClosingManager
    /// building a brand new one).
    /// </summary>
    private async Task NudgeClosingActualCashAsync(int dailyClosingId, decimal delta, CancellationToken cancellationToken)
    {
        var closing = await _dailyClosings.GetByIdAsync(dailyClosingId, cancellationToken);
        if (closing?.ActualCash is null)
            return;

        closing.ActualCash += delta;
        await _dailyClosings.UpdateAsync(closing, cancellationToken);
    }

    public async Task DeleteTransactionAsync(InvestorTransaction transaction, CancellationToken cancellationToken = default)
    {
        await _investors.DeleteTransactionAsync(transaction, cancellationToken);
    }

    private static InvestorDto ToDto(Investor investor) => new()
    {
        Id = investor.Id,
        Name = investor.Name,
        IsActive = investor.IsActive,
        CreatedAt = investor.CreatedAt
    };

    private static InvestorTransactionDto ToDto(InvestorTransaction transaction) => new()
    {
        Id = transaction.Id,
        Date = transaction.Date,
        Amount = transaction.Amount,
        TransactionType = transaction.TransactionType,
        Note = transaction.Note,
        DailyClosingId = transaction.DailyClosingId,
        InvestorId = transaction.InvestorId,
        InvestorName = transaction.Investor?.Name,
        ReceiverId = transaction.ReceiverId,
        ReceiverName = transaction.Receiver?.Name
    };
}
