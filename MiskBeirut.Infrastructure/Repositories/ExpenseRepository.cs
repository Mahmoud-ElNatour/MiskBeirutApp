using Microsoft.EntityFrameworkCore;
using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Repositories;
using MiskBeirut.Infrastructure.DbContexts;

namespace MiskBeirut.Infrastructure.Repositories;

public class ExpenseRepository : Repository<Expense>, IExpenseRepository
{
    public ExpenseRepository(MiskBeirutDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Expense>> GetByDailyClosingAsync(int dailyClosingId, CancellationToken cancellationToken = default)
        => await Context.Expenses
            .AsNoTracking()
            .Where(e => e.DailyClosingId == dailyClosingId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Expense>> GetByDateRangeAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
        => await Context.Expenses
            .AsNoTracking()
            .Where(e => e.Date >= from && e.Date <= to)
            .OrderBy(e => e.Date)
            .ToListAsync(cancellationToken);
}
