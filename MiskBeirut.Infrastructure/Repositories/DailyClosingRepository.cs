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
            .Include(d => d.Expenses).ThenInclude(e => e.Receiver)
            .Include(d => d.NonCashPayments)
            .Include(d => d.EmployeeLedgerEntries).ThenInclude(e => e.Employee)
            .Include(d => d.InvestorTransactions).ThenInclude(t => t.Investor)
            .Include(d => d.InvestorTransactions).ThenInclude(t => t.Receiver)
            .Include(d => d.CustomerLedgerEntries).ThenInclude(c => c.Customer)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
}
