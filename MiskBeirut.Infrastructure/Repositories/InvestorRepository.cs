using Microsoft.EntityFrameworkCore;
using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Enums;
using MiskBeirut.Core.Repositories;
using MiskBeirut.Infrastructure.DbContexts;

namespace MiskBeirut.Infrastructure.Repositories;

public class InvestorRepository : Repository<Investor>, IInvestorRepository
{
    public InvestorRepository(MiskBeirutDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Investor>> GetActiveAsync(CancellationToken cancellationToken = default)
        => await Context.Investors
            .AsNoTracking()
            .Where(i => i.IsActive)
            .OrderBy(i => i.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<InvestorTransaction>> GetTransactionsAsync(int investorId, CancellationToken cancellationToken = default)
        => await Context.InvestorTransactions
            .AsNoTracking()
            .Include(t => t.Receiver)
            .Where(t => t.InvestorId == investorId)
            .OrderByDescending(t => t.Date)
            .ToListAsync(cancellationToken);

    public async Task<InvestorTransaction> AddTransactionAsync(InvestorTransaction transaction, CancellationToken cancellationToken = default)
    {
        Context.InvestorTransactions.Add(transaction);
        await Context.SaveChangesAsync(cancellationToken);
        return transaction;
    }

    public async Task DeleteTransactionAsync(InvestorTransaction transaction, CancellationToken cancellationToken = default)
    {
        Context.InvestorTransactions.Remove(transaction);
        await Context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InvestorTransaction>> GetTransactionsByTypeAsync(InvestorTransactionType type, int? month, int? year, CancellationToken cancellationToken = default)
    {
        var query = Context.InvestorTransactions
            .AsNoTracking()
            .Include(t => t.Receiver)
            .Include(t => t.Investor)
            .Where(t => t.TransactionType == type);

        if (month.HasValue)
            query = query.Where(t => t.Date.Month == month.Value);
        if (year.HasValue)
            query = query.Where(t => t.Date.Year == year.Value);

        return await query.OrderByDescending(t => t.Date).ToListAsync(cancellationToken);
    }
}
