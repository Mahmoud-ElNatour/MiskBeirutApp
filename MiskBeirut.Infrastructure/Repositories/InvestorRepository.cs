using Microsoft.EntityFrameworkCore;
using MiskBeirut.Core.Entities;
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
            .Where(t => t.InvestorId == investorId)
            .OrderBy(t => t.Date)
            .ToListAsync(cancellationToken);

    public async Task<InvestorTransaction> AddTransactionAsync(InvestorTransaction transaction, CancellationToken cancellationToken = default)
    {
        Context.InvestorTransactions.Add(transaction);
        await Context.SaveChangesAsync(cancellationToken);
        return transaction;
    }
}
