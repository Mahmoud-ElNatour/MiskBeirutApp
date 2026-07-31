using MiskBeirut.Core.Entities;

namespace MiskBeirut.Core.Repositories;

public interface IGoogleReviewRepository : IRepository<GoogleReview>
{
    Task<IReadOnlyList<GoogleReview>> GetFeaturedAsync(int count, CancellationToken cancellationToken = default);
}
