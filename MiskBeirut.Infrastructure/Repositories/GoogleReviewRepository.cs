using Microsoft.EntityFrameworkCore;
using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Repositories;
using MiskBeirut.Infrastructure.DbContexts;

namespace MiskBeirut.Infrastructure.Repositories;

public class GoogleReviewRepository : Repository<GoogleReview>, IGoogleReviewRepository
{
    public GoogleReviewRepository(MiskBeirutDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<GoogleReview>> GetFeaturedAsync(int count, CancellationToken cancellationToken = default)
        => await Context.GoogleReviews
            .AsNoTracking()
            .OrderBy(r => r.DisplayOrder)
            .ThenByDescending(r => r.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
}
