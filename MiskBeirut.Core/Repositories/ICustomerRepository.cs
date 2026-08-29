using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Enums;

namespace MiskBeirut.Core.Repositories;

/// <summary>Back-office customers and their ledger (customer.customer_ledger).</summary>
public interface ICustomerRepository : IRepository<Customer>
{
    Task<Customer?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default);
    Task<Customer?> GetWithLedgerAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerLedger>> GetLedgerAsync(int customerId, CancellationToken cancellationToken = default);
    Task<CustomerLedger> AddLedgerEntryAsync(CustomerLedger entry, CancellationToken cancellationToken = default);

    /// <summary>Reverses the entry's effect on the customer's running balance, then removes it.</summary>
    Task DeleteLedgerEntryAsync(CustomerLedger entry, CancellationToken cancellationToken = default);

    /// <summary>Cross-customer ledger report (Credits or Cashbacks control-panel pages), with Customer loaded.</summary>
    Task<IReadOnlyList<CustomerLedger>> GetLedgerByTypeAsync(CustomerLedgerType type, int? month, int? year, CancellationToken cancellationToken = default);

    /// <summary>Manual ledger entries (DailyClosingId null) dated exactly <paramref name="date"/>, tracked so the caller can attach and save them.</summary>
    Task<IReadOnlyList<CustomerLedger>> GetUnattachedLedgerByDateAsync(DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>Saves changes made directly to a tracked ledger entry (e.g. attaching it to a Daily Closing) without touching the customer's balance — unlike <see cref="AddLedgerEntryAsync"/>/<see cref="DeleteLedgerEntryAsync"/>, which apply/reverse it.</summary>
    Task UpdateLedgerEntryAsync(CustomerLedger entry, CancellationToken cancellationToken = default);
}
