using Microsoft.EntityFrameworkCore;
using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Repositories;
using MiskBeirut.Infrastructure.DbContexts;

namespace MiskBeirut.Infrastructure.Repositories;

public class VacancyRepository : Repository<Vacancy>, IVacancyRepository
{
    public VacancyRepository(MiskBeirutDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Vacancy>> GetActiveAsync(CancellationToken cancellationToken = default)
        => await Context.Vacancies
            .AsNoTracking()
            .Where(v => v.IsActive)
            .OrderBy(v => v.DisplayOrder)
            .ThenBy(v => v.Id)
            .ToListAsync(cancellationToken);
}
