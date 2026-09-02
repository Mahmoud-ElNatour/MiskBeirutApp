using MiskBeirut.Core.Entities;

namespace MiskBeirut.Core.Repositories;

public interface IVacancyRepository : IRepository<Vacancy>
{
    /// <summary>
    /// Vacancies the public Careers page should show: active, and either without an application
    /// deadline or with one that hasn't passed yet.
    /// </summary>
    /// <param name="today">The current date in the caller's terms — the deadline day itself still counts as open.</param>
    Task<IReadOnlyList<Vacancy>> GetActiveAsync(DateTime today, CancellationToken cancellationToken = default);

    /// <summary>Every vacancy, active or not, in the order the Careers page would list them — for the Cms.</summary>
    Task<IReadOnlyList<Vacancy>> GetAllOrderedAsync(CancellationToken cancellationToken = default);

    /// <summary>True if another vacancy (any but <paramref name="exceptId"/>) already uses this slug.</summary>
    Task<bool> SlugExistsAsync(string slug, int exceptId, CancellationToken cancellationToken = default);

    /// <summary>How many job applications reference each vacancy, keyed by vacancy id.</summary>
    Task<IReadOnlyDictionary<int, int>> GetApplicationCountsAsync(CancellationToken cancellationToken = default);
}
