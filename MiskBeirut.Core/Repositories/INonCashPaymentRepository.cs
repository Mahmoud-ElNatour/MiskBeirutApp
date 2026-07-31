using MiskBeirut.Core.Entities;

namespace MiskBeirut.Core.Repositories;

public interface INonCashPaymentRepository : IRepository<NonCashPayment>
{
    Task<IReadOnlyList<NonCashPayment>> GetByDailyClosingAsync(int dailyClosingId, CancellationToken cancellationToken = default);
}
