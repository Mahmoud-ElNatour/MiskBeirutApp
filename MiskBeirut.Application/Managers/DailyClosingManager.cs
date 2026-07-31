using MiskBeirut.Application.Dtos.DailyClosings;
using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Enums;
using MiskBeirut.Core.Repositories;

namespace MiskBeirut.Application.Managers;

/// <summary>Daily cash-closing records and their aggregated totals.</summary>
public class DailyClosingManager
{
    private readonly IDailyClosingRepository _dailyClosings;

    public DailyClosingManager(IDailyClosingRepository dailyClosings)
    {
        _dailyClosings = dailyClosings;
    }

    public async Task<DailyClosingDto?> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var closing = await _dailyClosings.GetByDateAsync(date, cancellationToken);
        return closing is null ? null : ToDto(closing);
    }

    public async Task<DailyClosingDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var closing = await _dailyClosings.GetByIdAsync(id, cancellationToken);
        return closing is null ? null : ToDto(closing);
    }

    /// <summary>Lists closings for a period, most recent first. Null year/month returns everything.</summary>
    public async Task<IReadOnlyList<DailyClosingDto>> GetAllAsync(int? year = null, int? month = null, CancellationToken cancellationToken = default)
    {
        var closings = await _dailyClosings.GetAllAsync(cancellationToken);
        return closings
            .Where(c => year is null || c.Date.Year == year)
            .Where(c => month is null || c.Date.Month == month)
            .OrderByDescending(c => c.Date)
            .Select(ToDto)
            .ToList();
    }

    /// <summary>Edits the adjustable fields only — Date/MainReading anchor the unique-per-date row and aren't editable here.</summary>
    public async Task<DailyClosingDto> UpdateAsync(int id, UpdateDailyClosingRequest request, CancellationToken cancellationToken = default)
    {
        var closing = await _dailyClosings.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Daily closing {id} was not found.");

        closing.AdjustedReading = request.AdjustedReading;
        closing.ActualCash = request.ActualCash;
        closing.Note = request.Note;

        await _dailyClosings.UpdateAsync(closing, cancellationToken);
        return ToDto(closing);
    }

    /// <summary>
    /// Creates a closing for a date. The Date column is unique, so an existing
    /// closing for the same date is rejected here with a clear error.
    /// </summary>
    public async Task<DailyClosingDto> CreateAsync(CreateDailyClosingRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _dailyClosings.GetByDateAsync(request.Date, cancellationToken);
        if (existing is not null)
            throw new InvalidOperationException($"A daily closing for {request.Date:yyyy-MM-dd} already exists.");

        var closing = await _dailyClosings.AddAsync(new DailyClosing
        {
            Date = request.Date,
            MainReading = request.MainReading,
            AdjustedReading = request.AdjustedReading,
            ActualCash = request.ActualCash,
            Note = request.Note
        }, cancellationToken);

        return ToDto(closing);
    }

    /// <summary>
    /// Aggregates the day's movements. Employee ledger amounts are stored negative and only
    /// Advance entries count toward cash reconciliation — Deduct entries are excluded.
    /// </summary>
    public async Task<DailyClosingSummaryDto?> GetSummaryAsync(int id, CancellationToken cancellationToken = default)
    {
        var closing = await _dailyClosings.GetWithDetailsAsync(id, cancellationToken);
        if (closing is null)
            return null;

        return new DailyClosingSummaryDto
        {
            DailyClosingId = closing.Id,
            Date = closing.Date,
            MainReading = closing.MainReading,
            AdjustedReading = closing.AdjustedReading,
            ActualCash = closing.ActualCash,
            TotalExpenses = closing.Expenses.Sum(e => e.Amount),
            TotalNonCashPayments = closing.NonCashPayments.Sum(p => p.Amount),
            TotalEmployeeAdvances = closing.EmployeeLedgerEntries
                .Where(e => EmployeeManager.AffectsCashDrawer(e.Type))
                .Sum(e => e.Amount),
            TotalCustomerCredits = closing.CustomerLedgerEntries
                .Where(e => e.Type == CustomerLedgerType.Credit)
                .Sum(e => e.Amount),
            TotalCustomerCashbacks = closing.CustomerLedgerEntries
                .Where(e => e.Type == CustomerLedgerType.Cashback)
                .Sum(e => e.Amount),
            TotalInvestorWithdrawals = closing.InvestorTransactions
                .Where(t => t.TransactionType == InvestorTransactionType.Withdrawal)
                .Sum(t => t.Amount),
            TotalInvestorExpenses = closing.InvestorTransactions
                .Where(t => t.TransactionType == InvestorTransactionType.Expense)
                .Sum(t => t.Amount)
        };
    }

    private static DailyClosingDto ToDto(DailyClosing closing) => new()
    {
        Id = closing.Id,
        Date = closing.Date,
        MainReading = closing.MainReading,
        AdjustedReading = closing.AdjustedReading,
        ActualCash = closing.ActualCash,
        Note = closing.Note,
        CreatedAt = closing.CreatedAt
    };
}
