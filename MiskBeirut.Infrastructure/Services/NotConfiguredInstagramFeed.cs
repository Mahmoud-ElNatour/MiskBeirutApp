using MiskBeirut.Application.Dtos.Social;
using MiskBeirut.Application.Services;

namespace MiskBeirut.Infrastructure.Services;

/// <summary>
/// Stands in for <see cref="IInstagramFeed"/> until Instagram:UserId and Instagram:AccessToken are
/// set. Returns nothing rather than throwing, because the Home page treats an empty feed as its cue
/// to fall back to the photographs an editor uploaded through the Cms — which is exactly the right
/// behaviour before the Meta app exists, and the same behaviour if the token later expires.
///
/// Unlike the WhatsApp equivalent there is no error to surface: nobody clicks anything to trigger
/// this, so an unconfigured install should simply look like a site whose gallery is curated by hand.
/// </summary>
public sealed class NotConfiguredInstagramFeed : IInstagramFeed
{
    public Task<IReadOnlyList<InstagramPostDto>> GetRecentAsync(int count, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<InstagramPostDto>>([]);
}
