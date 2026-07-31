using Microsoft.EntityFrameworkCore;
using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Repositories;
using MiskBeirut.Infrastructure.DbContexts;

namespace MiskBeirut.Infrastructure.Repositories;

public class DailyClosingRepository : Repository<DailyClosing>, IDailyClosingRepository
{
    public DailyClosingRepository(MiskBeirutDbContext context) : base(context)
    {
    }

    public Task<DailyClosing?> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default)
        => Context.DailyClosings.FirstOrDefaultAsync(d => d.Date == date, cancellationToken);

    public Task<DailyClosing?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default)
        => Context.DailyClosings
            .Include(d => d.Expenses)
            .Include(d => d.NonCashPayments)
            .Include(d => d.EmployeeLedgerEntries)
            .Include(d => d.InvestorTransactions)
            .Include(d => d.CustomerLedgerEntries)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
}
