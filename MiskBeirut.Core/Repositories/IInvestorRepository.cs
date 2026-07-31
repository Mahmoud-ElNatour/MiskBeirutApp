using MiskBeirut.Core.Entities;

namespace MiskBeirut.Core.Repositories;

/// <summary>Investors and their transactions.</summary>
public interface IInvestorRepository : IRepository<Investor>
{
    Task<IReadOnlyList<Investor>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InvestorTransaction>> GetTransactionsAsync(int investorId, CancellationToken cancellationToken = default);
    Task<InvestorTransaction> AddTransactionAsync(InvestorTransaction transaction, CancellationToken cancellationToken = default);
}
