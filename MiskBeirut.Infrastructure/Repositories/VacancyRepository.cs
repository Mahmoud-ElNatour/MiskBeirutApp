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

    public async Task<IReadOnlyList<Vacancy>> GetActiveAsync(DateTime today, CancellationToken cancellationToken = default)
        => await Context.Vacancies
            .AsNoTracking()
            .Where(v => v.IsActive && (v.ApplicationDeadline == null || v.ApplicationDeadline >= today.Date))
            .OrderBy(v => v.DisplayOrder)
            .ThenBy(v => v.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Vacancy>> GetAllOrderedAsync(CancellationToken cancellationToken = default)
        => await Context.Vacancies
            .AsNoTracking()
            .OrderBy(v => v.DisplayOrder)
            .ThenBy(v => v.Id)
            .ToListAsync(cancellationToken);

    public async Task<bool> SlugExistsAsync(string slug, int exceptId, CancellationToken cancellationToken = default)
        => await Context.Vacancies.AnyAsync(v => v.Slug == slug && v.Id != exceptId, cancellationToken);

    public async Task<IReadOnlyDictionary<int, int>> GetApplicationCountsAsync(CancellationToken cancellationToken = default)
        => await Context.JobApplications
            .AsNoTracking()
            .GroupBy(a => a.VacancyId)
            .Select(g => new { VacancyId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.VacancyId, x => x.Count, cancellationToken);
}
