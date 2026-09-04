using MiskBeirut.Application.Dtos.Social;

namespace MiskBeirut.Application.Services;

/// <summary>
/// The restaurant's most recent Instagram posts, for the gallery row on the Home page.
///
/// Deliberately never throws. The gallery is decoration on a marketing page: if Instagram is slow,
/// the token has expired or the account has been switched back to personal, the right outcome is a
/// page that renders without it, not a home page that 500s. Callers get an empty list and the Home
/// page falls back to the photographs an editor uploaded through the Cms.
/// </summary>
public interface IInstagramFeed
{
    /// <param name="count">How many posts the caller wants. Fewer may be returned; never more.</param>
    Task<IReadOnlyList<InstagramPostDto>> GetRecentAsync(int count, CancellationToken cancellationToken = default);
}
