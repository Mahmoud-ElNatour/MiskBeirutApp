using MiskBeirut.Core.Entities;

namespace MiskBeirut.Core.Repositories;

/// <summary>Back-office customers and their ledger (customer.customer_ledger).</summary>
public interface ICustomerRepository : IRepository<Customer>
{
    Task<Customer?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default);
    Task<Customer?> GetWithLedgerAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerLedger>> GetLedgerAsync(int customerId, CancellationToken cancellationToken = default);
    Task<CustomerLedger> AddLedgerEntryAsync(CustomerLedger entry, CancellationToken cancellationToken = default);
}
