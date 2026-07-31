using MiskBeirut.Core.Entities;

namespace MiskBeirut.Core.Repositories;

public interface IDailyClosingRepository : IRepository<DailyClosing>
{
    Task<DailyClosing?> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>Loads the closing with its expenses, non-cash payments, ledger entries and investor transactions.</summary>
    Task<DailyClosing?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default);
}
