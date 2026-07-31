using MiskBeirut.Core.Entities;

namespace MiskBeirut.Core.Repositories;

public interface IExpenseRepository : IRepository<Expense>
{
    Task<IReadOnlyList<Expense>> GetByDailyClosingAsync(int dailyClosingId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Expense>> GetByDateRangeAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
}
