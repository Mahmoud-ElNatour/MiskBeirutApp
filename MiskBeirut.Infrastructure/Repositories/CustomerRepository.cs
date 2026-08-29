using Microsoft.EntityFrameworkCore;
using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Enums;
using MiskBeirut.Core.Repositories;
using MiskBeirut.Infrastructure.DbContexts;

namespace MiskBeirut.Infrastructure.Repositories;

public class CustomerRepository : Repository<Customer>, ICustomerRepository
{
    public CustomerRepository(MiskBeirutDbContext context) : base(context)
    {
    }

    public Task<Customer?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default)
        => Context.Customers.FirstOrDefaultAsync(c => c.PhoneNumber == phoneNumber, cancellationToken);

    public Task<Customer?> GetWithLedgerAsync(int id, CancellationToken cancellationToken = default)
        => Context.Customers
            .Include(c => c.LedgerEntries)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<CustomerLedger>> GetLedgerAsync(int customerId, CancellationToken cancellationToken = default)
        => await Context.CustomerLedgers
            .AsNoTracking()
            .Where(l => l.CustomerId == customerId)
            .OrderBy(l => l.Date)
            .ToListAsync(cancellationToken);

    /// <summary>Inserts the ledger entry and applies it to the customer's running balance in the same save.</summary>
    public async Task<CustomerLedger> AddLedgerEntryAsync(CustomerLedger entry, CancellationToken cancellationToken = default)
    {
        var customer = await Context.Customers.FirstAsync(c => c.Id == entry.CustomerId, cancellationToken);
        customer.Balance += entry.Amount;

        Context.CustomerLedgers.Add(entry);
        await Context.SaveChangesAsync(cancellationToken);
        return entry;
    }

    /// <summary>Reverses the entry's effect on the customer's running balance, then removes it.</summary>
    public async Task DeleteLedgerEntryAsync(CustomerLedger entry, CancellationToken cancellationToken = default)
    {
        var customer = await Context.Customers.FirstAsync(c => c.Id == entry.CustomerId, cancellationToken);
        customer.Balance -= entry.Amount;

        Context.CustomerLedgers.Remove(entry);
        await Context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CustomerLedger>> GetLedgerByTypeAsync(CustomerLedgerType type, int? month, int? year, CancellationToken cancellationToken = default)
    {
        var query = Context.CustomerLedgers
            .AsNoTracking()
            .Include(l => l.Customer)
            .Where(l => l.Type == type);

        if (month.HasValue)
            query = query.Where(l => l.Date.Month == month.Value);
        if (year.HasValue)
            query = query.Where(l => l.Date.Year == year.Value);

        return await query.OrderByDescending(l => l.Date).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CustomerLedger>> GetUnattachedLedgerByDateAsync(DateOnly date, CancellationToken cancellationToken = default)
        => await Context.CustomerLedgers
            .Where(l => l.DailyClosingId == null && l.Date == date)
            .ToListAsync(cancellationToken);

    public async Task UpdateLedgerEntryAsync(CustomerLedger entry, CancellationToken cancellationToken = default)
    {
        Context.CustomerLedgers.Update(entry);
        await Context.SaveChangesAsync(cancellationToken);
    }
}
