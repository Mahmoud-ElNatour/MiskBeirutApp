using MiskBeirut.Application.Dtos.Reviews;
using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Repositories;

namespace MiskBeirut.Application.Managers;

/// <summary>Google Maps reviews, populated externally and shown on the homepage.</summary>
public class GoogleReviewManager
{
    private readonly IGoogleReviewRepository _reviews;

    public GoogleReviewManager(IGoogleReviewRepository reviews)
    {
        _reviews = reviews;
    }

    public async Task<IReadOnlyList<GoogleReviewDto>> GetFeaturedAsync(int count, CancellationToken cancellationToken = default)
    {
        var reviews = await _reviews.GetFeaturedAsync(count, cancellationToken);
        return reviews.Select(ToDto).ToList();
    }

    private static GoogleReviewDto ToDto(GoogleReview review) => new()
    {
        Id = review.Id,
        AuthorName = review.AuthorName,
        ProfilePhotoUrl = review.ProfilePhotoUrl,
        Rating = review.Rating,
        ReviewText = review.ReviewText,
        RelativeTime = review.RelativeTime
    };
}
