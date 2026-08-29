using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Enums;

namespace MiskBeirut.Core.Repositories;

/// <summary>Investors and their transactions.</summary>
public interface IInvestorRepository : IRepository<Investor>
{
    Task<IReadOnlyList<Investor>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InvestorTransaction>> GetTransactionsAsync(int investorId, CancellationToken cancellationToken = default);
    Task<InvestorTransaction> AddTransactionAsync(InvestorTransaction transaction, CancellationToken cancellationToken = default);
    Task DeleteTransactionAsync(InvestorTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>Cross-investor transactions of one type (Reports page's investor-expense breakdown), with Receiver loaded.</summary>
    Task<IReadOnlyList<InvestorTransaction>> GetTransactionsByTypeAsync(InvestorTransactionType type, int? month, int? year, CancellationToken cancellationToken = default);
}
