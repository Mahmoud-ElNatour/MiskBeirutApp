using MiskBeirut.Application.Dtos.Customers;
using MiskBeirut.Application.Dtos.DailyClosings;
using MiskBeirut.Application.Dtos.Employees;
using MiskBeirut.Application.Dtos.Investors;
using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Enums;
using MiskBeirut.Core.Repositories;

namespace MiskBeirut.Application.Managers;

/// <summary>Daily cash-closing records and their aggregated totals.</summary>
public class DailyClosingManager
{
    private readonly IDailyClosingRepository _dailyClosings;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ExpenseManager _expenses;
    private readonly InvestorManager _investors;
    private readonly EmployeeManager _employees;
    private readonly CustomerManager _customers;
    private readonly NonCashPaymentManager _nonCashPayments;

    public DailyClosingManager(
        IDailyClosingRepository dailyClosings,
        IUnitOfWork unitOfWork,
        ExpenseManager expenses,
        InvestorManager investors,
        EmployeeManager employees,
        CustomerManager customers,
        NonCashPaymentManager nonCashPayments)
    {
        _dailyClosings = dailyClosings;
        _unitOfWork = unitOfWork;
        _expenses = expenses;
        _investors = investors;
        _employees = employees;
        _customers = customers;
        _nonCashPayments = nonCashPayments;
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
    /// Creates a closing header plus every line-item row entered for it (general expenses, investor
    /// expenses, employee advances/deductions, customer credits/cashbacks, non-cash payments) as one
    /// atomic operation — nothing persists unless the whole thing succeeds. AdjustedReading/ActualCash
    /// aren't taken from the caller; they're derived here from the rows actually inserted:
    /// <list type="bullet">
    /// <item>AdjustedReading = MainReading − ΣCredits + ΣCashbacks</item>
    /// <item>ActualCash = AdjustedReading − ΣGeneralExpenses − ΣInvestorExpenses − ΣAdvances − ΣNonCashPayments
    /// (Deductions are excluded, matching <see cref="EmployeeManager.AffectsCashDrawer"/>'s
    /// Advance-only convention used elsewhere in this class.)</item>
    /// </list>
    /// </summary>
    public async Task<DailyClosingDto> CreateWithLinesAsync(CreateDailyClosingWithLinesRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _dailyClosings.GetByDateAsync(request.Date, cancellationToken);
        if (existing is not null)
            throw new InvalidOperationException($"A daily closing for {request.Date:yyyy-MM-dd} already exists.");

        DailyClosing closing = null!;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            closing = await _dailyClosings.AddAsync(new DailyClosing
            {
                Date = request.Date,
                MainReading = request.MainReading,
                Note = request.Note
            }, cancellationToken);

            await AddLinesAsync(closing.Id, request.Date, request.GeneralExpenses, request.InvestorExpenses,
                request.Advances, request.Deductions, request.Credits, request.Cashbacks, request.NonCashPayments, cancellationToken);
            await AttachManualEntriesAsync(request.Date, closing.Id, cancellationToken);

            var totals = await ComputeTotalsAsync(closing.Id, cancellationToken);
            ApplyComputedTotals(closing, request.MainReading, totals);
            await _dailyClosings.UpdateAsync(closing, cancellationToken);
        }, cancellationToken);

        return ToDto(closing);
    }

    /// <summary>
    /// Replaces a closing's header and every line item wholesale: deletes every existing Expense,
    /// InvestorTransaction, EmployeeLedger and CustomerLedger row tied to it, then re-adds everything
    /// from <paramref name="request"/> — all in one transaction. Simpler and safer than diffing
    /// row-by-row (nothing to reconcile by Id), at the cost of child rows getting new Ids on every
    /// edit; nothing in this app treats those Ids as stable across an edit.
    /// </summary>
    public async Task<DailyClosingDto> UpdateWithLinesAsync(int id, UpdateDailyClosingWithLinesRequest request, CancellationToken cancellationToken = default)
    {
        var clashing = await _dailyClosings.GetByDateAsync(request.Date, cancellationToken);
        if (clashing is not null && clashing.Id != id)
            throw new InvalidOperationException($"A daily closing for {request.Date:yyyy-MM-dd} already exists.");

        DailyClosing closing = null!;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            closing = await _dailyClosings.GetWithDetailsAsync(id, cancellationToken)
                ?? throw new InvalidOperationException($"Daily closing {id} was not found.");

            // Manual expenses/credits/cashbacks (IsManualEntry) aren't part of this closing's
            // submitted line items — they were entered standalone and attached automatically (see
            // AttachManualEntriesAsync) — so they're left alone here instead of being wiped and
            // needing to be resubmitted. Everything else is deleted and rebuilt wholesale below.
            foreach (var e in closing.Expenses.Where(e => !e.IsManualEntry).ToList())
                await _expenses.DeleteAsync(e.Id, cancellationToken);
            foreach (var p in closing.NonCashPayments.ToList())
                await _nonCashPayments.DeleteAsync(p.Id, cancellationToken);
            foreach (var t in closing.InvestorTransactions.ToList())
                await _investors.DeleteTransactionAsync(t, cancellationToken);
            foreach (var l in closing.EmployeeLedgerEntries.ToList())
                await _employees.DeleteLedgerEntryAsync(l, cancellationToken);
            foreach (var c in closing.CustomerLedgerEntries.Where(c => !c.IsManualEntry).ToList())
                await _customers.DeleteLedgerEntryAsync(c, cancellationToken);

            await AddLinesAsync(closing.Id, request.Date, request.GeneralExpenses, request.InvestorExpenses,
                request.Advances, request.Deductions, request.Credits, request.Cashbacks, request.NonCashPayments, cancellationToken);
            await AttachManualEntriesAsync(request.Date, closing.Id, cancellationToken);

            closing.Date = request.Date;
            closing.Note = request.Note;
            var totals = await ComputeTotalsAsync(closing.Id, cancellationToken);
            ApplyComputedTotals(closing, request.MainReading, totals);
            await _dailyClosings.UpdateAsync(closing, cancellationToken);
        }, cancellationToken);

        return ToDto(closing);
    }

    /// <summary>Attaches any same-date manual expense/credit/cashback (no Daily Closing yet) to this closing — see ExpenseManager/CustomerManager.AttachManualEntriesAsync.</summary>
    private async Task AttachManualEntriesAsync(DateOnly date, int dailyClosingId, CancellationToken cancellationToken)
    {
        await _expenses.AttachManualEntriesAsync(date, dailyClosingId, cancellationToken);
        await _customers.AttachManualEntriesAsync(date, dailyClosingId, cancellationToken);
    }

    /// <summary>
    /// Recomputes each section's total directly from the closing's actual persisted rows (rather
    /// than from just-submitted request lines), so manual entries attached via
    /// <see cref="AttachManualEntriesAsync"/> — whether newly attached this call or left in place
    /// from a previous one — are counted correctly alongside the resubmitted line items.
    /// </summary>
    private async Task<LineTotals> ComputeTotalsAsync(int dailyClosingId, CancellationToken cancellationToken)
    {
        var closing = await _dailyClosings.GetWithDetailsAsync(dailyClosingId, cancellationToken)
            ?? throw new InvalidOperationException($"Daily closing {dailyClosingId} was not found.");

        return new LineTotals(
            GeneralExpenses: closing.Expenses.Sum(e => e.Amount),
            InvestorExpenses: closing.InvestorTransactions.Where(t => t.TransactionType == InvestorTransactionType.Expense).Sum(t => t.Amount),
            Advances: closing.EmployeeLedgerEntries.Where(e => EmployeeManager.AffectsCashDrawer(e.Type)).Sum(e => Math.Abs(e.Amount)),
            Credits: closing.CustomerLedgerEntries.Where(c => c.Type == CustomerLedgerType.Credit).Sum(c => Math.Abs(c.Amount)),
            Cashbacks: closing.CustomerLedgerEntries.Where(c => c.Type == CustomerLedgerType.Cashback).Sum(c => c.Amount),
            NonCashPayments: closing.NonCashPayments.Sum(p => p.Amount));
    }

    /// <summary>Adds every submitted line-item row for a closing, tagging each with its Date/DailyClosingId.
    /// Totals are derived afterward from the closing's actual persisted rows — see <see cref="ComputeTotalsAsync"/>.</summary>
    private async Task AddLinesAsync(
        int dailyClosingId,
        DateOnly date,
        IReadOnlyList<GeneralExpenseLine> generalExpenses,
        IReadOnlyList<InvestorExpenseLine> investorExpenses,
        IReadOnlyList<EmployeeLedgerLine> advances,
        IReadOnlyList<EmployeeLedgerLine> deductions,
        IReadOnlyList<CustomerLedgerLine> credits,
        IReadOnlyList<CustomerLedgerLine> cashbacks,
        IReadOnlyList<NonCashPaymentLine> nonCashPayments,
        CancellationToken cancellationToken)
    {
        foreach (var line in generalExpenses)
        {
            await _expenses.AddAsync(new CreateExpenseRequest
            {
                Date = date,
                Amount = line.Amount,
                Note = line.Note,
                DailyClosingId = dailyClosingId,
                ReceiverId = line.ReceiverId
            }, cancellationToken);
        }

        foreach (var line in investorExpenses)
        {
            await _investors.AddTransactionAsync(new CreateInvestorTransactionRequest
            {
                Date = date,
                Amount = line.Amount,
                TransactionType = InvestorTransactionType.Expense,
                Note = line.Note,
                DailyClosingId = dailyClosingId,
                InvestorId = line.InvestorId,
                ReceiverId = line.ReceiverId
            }, cancellationToken);
        }

        foreach (var line in advances)
        {
            await _employees.AddLedgerEntryAsync(new CreateEmployeeLedgerEntryRequest
            {
                Date = date,
                Amount = -Math.Abs(line.Amount),
                Type = EmployeeLedgerType.Advance,
                Note = line.Note,
                EmployeeId = line.EmployeeId,
                DailyClosingId = dailyClosingId
            }, cancellationToken);
        }

        foreach (var line in deductions)
        {
            await _employees.AddLedgerEntryAsync(new CreateEmployeeLedgerEntryRequest
            {
                Date = date,
                Amount = -Math.Abs(line.Amount),
                Type = EmployeeLedgerType.Deduct,
                Note = line.Note,
                EmployeeId = line.EmployeeId,
                DailyClosingId = dailyClosingId
            }, cancellationToken);
        }

        foreach (var line in credits)
        {
            await _customers.AddLedgerEntryAsync(new CreateCustomerLedgerEntryRequest
            {
                Date = date,
                Amount = -Math.Abs(line.Amount),
                Type = CustomerLedgerType.Credit,
                Note = line.Note,
                CustomerId = line.CustomerId,
                DailyClosingId = dailyClosingId
            }, cancellationToken);
        }

        foreach (var line in cashbacks)
        {
            await _customers.AddLedgerEntryAsync(new CreateCustomerLedgerEntryRequest
            {
                Date = date,
                Amount = Math.Abs(line.Amount),
                Type = CustomerLedgerType.Cashback,
                Note = line.Note,
                CustomerId = line.CustomerId,
                DailyClosingId = dailyClosingId
            }, cancellationToken);
        }

        foreach (var line in nonCashPayments)
        {
            await _nonCashPayments.AddAsync(new CreateNonCashPaymentRequest
            {
                Date = date,
                Amount = line.Amount,
                PaymentMethod = line.PaymentMethod,
                Note = line.Note,
                DailyClosingId = dailyClosingId
            }, cancellationToken);
        }
    }

    /// <summary>
    /// AdjustedReading = MainReading − ΣCredits + ΣCashbacks.
    /// ActualCash = AdjustedReading − ΣGeneralExpenses − ΣInvestorExpenses − ΣAdvances − ΣNonCashPayments
    /// (Deductions are excluded, matching <see cref="EmployeeManager.AffectsCashDrawer"/>'s
    /// Advance-only convention used elsewhere in this class.)
    /// </summary>
    private static void ApplyComputedTotals(DailyClosing closing, decimal mainReading, LineTotals totals)
    {
        closing.MainReading = mainReading;
        var adjustedReading = mainReading - totals.Credits + totals.Cashbacks;
        closing.AdjustedReading = adjustedReading;
        closing.ActualCash = adjustedReading - totals.GeneralExpenses - totals.InvestorExpenses - totals.Advances - totals.NonCashPayments;
    }

    private readonly record struct LineTotals(
        decimal GeneralExpenses,
        decimal InvestorExpenses,
        decimal Advances,
        decimal Credits,
        decimal Cashbacks,
        decimal NonCashPayments);

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

    /// <summary>
    /// The named Advances/Deductions/Credits/Cashbacks/Investor-expense entries for one closing —
    /// the detail sections on the Daily Close details/print page.
    /// </summary>
    public async Task<DailyClosingBreakdownDto?> GetBreakdownAsync(int id, CancellationToken cancellationToken = default)
    {
        var closing = await _dailyClosings.GetWithDetailsAsync(id, cancellationToken);
        if (closing is null)
            return null;

        return new DailyClosingBreakdownDto
        {
            Advances = closing.EmployeeLedgerEntries
                .Where(e => e.Type == EmployeeLedgerType.Advance)
                .Select(ToDto)
                .ToList(),
            Deductions = closing.EmployeeLedgerEntries
                .Where(e => e.Type == EmployeeLedgerType.Deduct)
                .Select(ToDto)
                .ToList(),
            Credits = closing.CustomerLedgerEntries
                .Where(c => c.Type == CustomerLedgerType.Credit)
                .Select(ToDto)
                .ToList(),
            Cashbacks = closing.CustomerLedgerEntries
                .Where(c => c.Type == CustomerLedgerType.Cashback)
                .Select(ToDto)
                .ToList(),
            InvestorExpenses = closing.InvestorTransactions
                .Where(t => t.TransactionType == InvestorTransactionType.Expense)
                .Select(ToDto)
                .ToList()
        };
    }

    private static EmployeeLedgerReportEntryDto ToDto(EmployeeLedger entry) => new()
    {
        Id = entry.Id,
        Date = entry.Date,
        Amount = entry.Amount,
        Type = entry.Type,
        Note = entry.Note,
        EmployeeId = entry.EmployeeId,
        EmployeeName = entry.Employee?.Name ?? "Unassigned"
    };

    private static CustomerLedgerReportEntryDto ToDto(CustomerLedger entry) => new()
    {
        Id = entry.Id,
        Date = entry.Date,
        Amount = entry.Amount,
        Type = entry.Type,
        Note = entry.Note,
        CustomerId = entry.CustomerId,
        CustomerName = entry.Customer?.Name ?? "Unassigned"
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
        ReceiverId = transaction.ReceiverId,
        ReceiverName = transaction.Receiver?.Name,
        InvestorName = transaction.Investor?.Name ?? "Unassigned"
    };

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
