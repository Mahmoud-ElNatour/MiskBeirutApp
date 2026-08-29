using MiskBeirut.Core.Entities;

namespace MiskBeirut.Core.Repositories;

public interface IVacancyRepository : IRepository<Vacancy>
{
    Task<IReadOnlyList<Vacancy>> GetActiveAsync(CancellationToken cancellationToken = default);
}
