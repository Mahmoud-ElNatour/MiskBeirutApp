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

    public async Task<IReadOnlyList<Expense>> GetUnattachedByDateAsync(DateOnly date, CancellationToken cancellationToken = default)
        => await Context.Expenses
            .Where(e => e.DailyClosingId == null && e.Date == date)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Expense>> GetReportAsync(int? month, int? year, int? receiverId, CancellationToken cancellationToken = default)
    {
        var query = Context.Expenses
            .AsNoTracking()
            .Include(e => e.Receiver)
            .AsQueryable();

        if (month.HasValue)
            query = query.Where(e => e.Date.Month == month.Value);
        if (year.HasValue)
            query = query.Where(e => e.Date.Year == year.Value);
        if (receiverId.HasValue)
            query = query.Where(e => e.ReceiverId == receiverId.Value);

        return await query.OrderByDescending(e => e.Date).ToListAsync(cancellationToken);
    }
}
