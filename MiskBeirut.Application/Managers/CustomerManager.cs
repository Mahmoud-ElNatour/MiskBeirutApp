using MiskBeirut.Application.Dtos.Customers;
using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Enums;
using MiskBeirut.Core.Repositories;

namespace MiskBeirut.Application.Managers;

/// <summary>Back-office customer accounts and their credit/cashback ledger.</summary>
public class CustomerManager
{
    private readonly ICustomerRepository _customers;
    private readonly IDailyClosingRepository _dailyClosings;

    // Depends on IDailyClosingRepository directly rather than DailyClosingManager — the latter
    // already depends on CustomerManager (it posts each closing's credits/cashbacks through it),
    // so going the other way would be a circular dependency.
    public CustomerManager(ICustomerRepository customers, IDailyClosingRepository dailyClosings)
    {
        _customers = customers;
        _dailyClosings = dailyClosings;
    }

    public async Task<IReadOnlyList<CustomerDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var customers = await _customers.GetAllAsync(cancellationToken);
        return customers.Select(ToDto).ToList();
    }

    public async Task<CustomerDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var customer = await _customers.GetByIdAsync(id, cancellationToken);
        return customer is null ? null : ToDto(customer);
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await _customers.AddAsync(new Customer
        {
            Name = request.Name,
            PhoneNumber = request.PhoneNumber,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        return ToDto(customer);
    }

    public async Task<CustomerDto> UpdateAsync(int id, UpdateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await _customers.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Customer {id} was not found.");

        customer.Name = request.Name;
        customer.PhoneNumber = request.PhoneNumber;

        await _customers.UpdateAsync(customer, cancellationToken);
        return ToDto(customer);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var customer = await _customers.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Customer {id} was not found.");

        await _customers.DeleteAsync(customer, cancellationToken);
    }

    /// <summary>
    /// Directly overwrites the stored balance. Used by the legacy Admin API surface, which
    /// treats Balance as a plain editable field rather than a value derived from ledger entries.
    /// </summary>
    public async Task<CustomerDto> SetBalanceAsync(int id, decimal balance, CancellationToken cancellationToken = default)
    {
        var customer = await _customers.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Customer {id} was not found.");

        customer.Balance = balance;
        await _customers.UpdateAsync(customer, cancellationToken);
        return ToDto(customer);
    }

    /// <summary>
    /// Applies a balance edit made directly on the Customers page (the "Balance" field in its edit
    /// modal) as a proper Credit/Cashback ledger entry dated today, instead of silently overwriting
    /// the stored balance with no trail. The difference from the current balance is recorded as a
    /// Credit (negative) if it's a decrease or a Cashback (positive) if it's an increase — same sign
    /// rule <see cref="AddLedgerEntryAsync"/> enforces for entries added from the Details page.
    /// If today's Daily Closing doesn't exist yet, the entry is saved as a manual entry with no
    /// closing attached — <see cref="Managers.DailyClosingManager"/> folds it in once one is created
    /// or edited for today. No-ops (returns the customer unchanged) if the balance isn't actually
    /// different.
    /// </summary>
    public async Task<CustomerDto> AdjustBalanceAsync(int id, decimal newBalance, string? note, CancellationToken cancellationToken = default)
    {
        var customer = await _customers.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Customer {id} was not found.");

        var delta = newBalance - customer.Balance;
        if (delta == 0)
            return ToDto(customer);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var todaysClosing = await _dailyClosings.GetByDateAsync(today, cancellationToken);

        // AddLedgerEntryAsync applies `delta` to this same tracked `customer` instance as part of
        // saving the entry (see CustomerRepository), so `customer` reflects the new balance right
        // after this call — no need to re-fetch.
        await _customers.AddLedgerEntryAsync(new CustomerLedger
        {
            Date = today,
            Amount = delta,
            Type = delta < 0 ? CustomerLedgerType.Credit : CustomerLedgerType.Cashback,
            Note = note ?? "Balance adjusted from the Customers page.",
            CustomerId = id,
            DailyClosingId = todaysClosing?.Id,
            IsManualEntry = true
        }, cancellationToken);

        if (todaysClosing is not null)
            await NudgeClosingTotalsAsync(todaysClosing.Id, delta, cancellationToken);

        return ToDto(customer);
    }

    /// <summary>
    /// AdjustedReading/ActualCash are computed once (MainReading − ΣCredits + ΣCashbacks, then minus
    /// expenses/advances/non-cash) when a closing is created or edited via the New Close/Edit Close
    /// forms — see DailyClosingManager.ApplyComputedTotals — and stored as plain columns rather than
    /// derived live. A Credit/Cashback added to an existing closing afterward, from either this
    /// class's <see cref="AdjustBalanceAsync"/> or <see cref="AddLedgerEntryAsync"/>, has to nudge
    /// those same two stored totals by <paramref name="delta"/> (no expense/advance/non-cash total is
    /// affected by a credit/cashback — both move by exactly the entry's amount), or the Sales
    /// Dashboard keeps showing pre-edit numbers. No-ops if the closing has no computed totals yet
    /// (e.g. mid-way through DailyClosingManager building a brand new one — ApplyComputedTotals sets
    /// the real values once every line is in).
    /// </summary>
    private async Task NudgeClosingTotalsAsync(int dailyClosingId, decimal delta, CancellationToken cancellationToken)
    {
        var closing = await _dailyClosings.GetByIdAsync(dailyClosingId, cancellationToken);
        if (closing is null)
            return;

        if (closing.AdjustedReading.HasValue)
            closing.AdjustedReading += delta;
        if (closing.ActualCash.HasValue)
            closing.ActualCash += delta;
        await _dailyClosings.UpdateAsync(closing, cancellationToken);
    }

    public async Task<IReadOnlyList<CustomerLedgerEntryDto>> GetLedgerAsync(int customerId, CancellationToken cancellationToken = default)
    {
        var entries = await _customers.GetLedgerAsync(customerId, cancellationToken);
        return entries.Select(ToDto).ToList();
    }

    /// <summary>Cross-customer report for the Control Panel's Credits/Cashbacks pages.</summary>
    public async Task<IReadOnlyList<CustomerLedgerReportEntryDto>> GetLedgerReportAsync(CustomerLedgerType type, int? month, int? year, CancellationToken cancellationToken = default)
    {
        var entries = await _customers.GetLedgerByTypeAsync(type, month, year, cancellationToken);
        return entries.Select(e => new CustomerLedgerReportEntryDto
        {
            Id = e.Id,
            Date = e.Date,
            Amount = e.Amount,
            Type = e.Type,
            Note = e.Note,
            CustomerId = e.CustomerId,
            CustomerName = e.Customer.Name
        }).ToList();
    }

    /// <summary>
    /// Adds a ledger entry after validating the sign rule: Credit entries must be negative,
    /// Cashback entries must be positive. Validated here so violations surface as clear
    /// errors instead of database check-constraint exceptions.
    /// </summary>
    public async Task<CustomerLedgerEntryDto> AddLedgerEntryAsync(CreateCustomerLedgerEntryRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Type == CustomerLedgerType.Credit && request.Amount >= 0)
            throw new InvalidOperationException("Credit ledger entries must have a negative amount.");
        if (request.Type == CustomerLedgerType.Cashback && request.Amount <= 0)
            throw new InvalidOperationException("Cashback ledger entries must have a positive amount.");

        _ = await _customers.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new InvalidOperationException($"Customer {request.CustomerId} was not found.");

        var entry = await _customers.AddLedgerEntryAsync(new CustomerLedger
        {
            Date = request.Date,
            Amount = request.Amount,
            Type = request.Type,
            Note = request.Note,
            CustomerId = request.CustomerId,
            DailyClosingId = request.DailyClosingId,
            IsManualEntry = request.IsManualEntry
        }, cancellationToken);

        if (request.DailyClosingId is int closingId)
            await NudgeClosingTotalsAsync(closingId, request.Amount, cancellationToken);

        return ToDto(entry);
    }

    public async Task DeleteLedgerEntryAsync(CustomerLedger entry, CancellationToken cancellationToken = default)
    {
        await _customers.DeleteLedgerEntryAsync(entry, cancellationToken);
    }

    /// <summary>
    /// Attaches every manual credit/cashback (see <c>CustomerLedger.IsManualEntry</c>) dated
    /// <paramref name="date"/> that has no Daily Closing yet to <paramref name="dailyClosingId"/> —
    /// called by <see cref="DailyClosingManager"/> whenever a closing for that date is created or
    /// edited, so a manual entry entered ahead of the closing still folds into its
    /// AdjustedReading/ActualCash totals. Returns the summed Credit/Cashback magnitudes attached, for
    /// the caller's own totals recompute (no per-entry nudge here).
    /// </summary>
    public async Task<(decimal Credits, decimal Cashbacks)> AttachManualEntriesAsync(DateOnly date, int dailyClosingId, CancellationToken cancellationToken = default)
    {
        var unattached = await _customers.GetUnattachedLedgerByDateAsync(date, cancellationToken);
        decimal credits = 0, cashbacks = 0;
        foreach (var entry in unattached)
        {
            entry.DailyClosingId = dailyClosingId;
            await _customers.UpdateLedgerEntryAsync(entry, cancellationToken);
            if (entry.Type == CustomerLedgerType.Credit)
                credits += Math.Abs(entry.Amount);
            else
                cashbacks += entry.Amount;
        }
        return (credits, cashbacks);
    }

    private static CustomerDto ToDto(Customer customer) => new()
    {
        Id = customer.Id,
        Name = customer.Name,
        PhoneNumber = customer.PhoneNumber,
        Balance = customer.Balance,
        CreatedAt = customer.CreatedAt
    };

    private static CustomerLedgerEntryDto ToDto(CustomerLedger entry) => new()
    {
        Id = entry.Id,
        Date = entry.Date,
        Amount = entry.Amount,
        Type = entry.Type,
        Note = entry.Note,
        CustomerId = entry.CustomerId,
        DailyClosingId = entry.DailyClosingId,
        IsManualEntry = entry.IsManualEntry
    };
}
