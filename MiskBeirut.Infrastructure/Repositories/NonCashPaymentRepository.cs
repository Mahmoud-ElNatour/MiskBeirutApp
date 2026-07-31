using Microsoft.EntityFrameworkCore;
using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Repositories;
using MiskBeirut.Infrastructure.DbContexts;

namespace MiskBeirut.Infrastructure.Repositories;

public class NonCashPaymentRepository : Repository<NonCashPayment>, INonCashPaymentRepository
{
    public NonCashPaymentRepository(MiskBeirutDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<NonCashPayment>> GetByDailyClosingAsync(int dailyClosingId, CancellationToken cancellationToken = default)
        => await Context.NonCashPayments
            .AsNoTracking()
            .Where(p => p.DailyClosingId == dailyClosingId)
            .ToListAsync(cancellationToken);
}
