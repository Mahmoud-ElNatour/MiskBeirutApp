using MiskBeirut.Core.Entities;

namespace MiskBeirut.Core.Repositories;

public interface IExpenseRepository : IRepository<Expense>
{
    Task<IReadOnlyList<Expense>> GetByDailyClosingAsync(int dailyClosingId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Expense>> GetByDateRangeAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    /// <summary>Manual expenses (DailyClosingId null) dated exactly <paramref name="date"/>, tracked so the caller can attach and save them.</summary>
    Task<IReadOnlyList<Expense>> GetUnattachedByDateAsync(DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>Cross-receiver report (Control Panel's Expenses page), with Receiver loaded.</summary>
    Task<IReadOnlyList<Expense>> GetReportAsync(int? month, int? year, int? receiverId, CancellationToken cancellationToken = default);
}
