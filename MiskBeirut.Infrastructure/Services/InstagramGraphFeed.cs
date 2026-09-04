using System.Text.Json;
using Microsoft.Extensions.Logging;
using MiskBeirut.Application.Dtos.Social;
using MiskBeirut.Application.Services;

namespace MiskBeirut.Infrastructure.Services;

/// <summary>
/// Reads the account's recent posts from Meta's Graph API.
///
/// This is the only supported way to get a post's image. Reading it off a pasted instagram.com link
/// does not work: the post pages return no og:image and no media URLs, because the content is
/// rendered client-side, and the old public oEmbed endpoint was withdrawn. Anything that appears to
/// work by scraping those pages breaks the next time Instagram changes its markup, and is against
/// their terms in the meantime — so the account is read through the API it publishes, with a token.
///
/// Never throws. The gallery is decoration on a marketing page, so a failure here degrades to the
/// photographs an editor uploaded through the Cms rather than taking the home page down with it.
/// </summary>
public sealed class InstagramGraphFeed : IInstagramFeed
{
    /// <summary>Fields worth asking for. Requesting media_url on a video returns the video file, hence thumbnail_url alongside it.</summary>
    private const string Fields = "id,media_type,media_url,thumbnail_url,permalink,caption,timestamp";

    /// <summary>
    /// How long to wait before trying again after a failure. Deliberately much shorter than the
    /// success cache — a transient blip should heal in minutes — but long enough that an expired
    /// token doesn't mean an outbound HTTP call on every single page view.
    /// </summary>
    private static readonly TimeSpan RetryAfterFailure = TimeSpan.FromMinutes(5);

    private readonly HttpClient _http;
    private readonly string _userId;
    private readonly string _accessToken;
    private readonly string _apiBaseUrl;
    private readonly string _apiVersion;
    private readonly int _fetchCount;
    private readonly TimeSpan _cacheFor;
    private readonly ILogger<InstagramGraphFeed> _logger;

    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    /// <summary>
    /// Held in memory only, and never written to the database. The image URLs Graph returns are
    /// signed CDN links that expire after a matter of days, so a stored copy would quietly turn into
    /// a row of broken images — the cache window has to stay comfortably shorter than their
    /// lifetime, which is why Instagram:CacheMinutes is measured in minutes rather than days.
    /// </summary>
    private IReadOnlyList<InstagramPostDto> _cached = [];
    private DateTimeOffset _nextRefresh = DateTimeOffset.MinValue;

    public InstagramGraphFeed(
        HttpClient http,
        string userId,
        string accessToken,
        string apiBaseUrl,
        string apiVersion,
        int fetchCount,
        TimeSpan cacheFor,
        ILogger<InstagramGraphFeed> logger)
    {
        _http = http;
        _userId = userId;
        _accessToken = accessToken;
        _apiBaseUrl = apiBaseUrl.TrimEnd('/');
        _apiVersion = apiVersion;
        _fetchCount = fetchCount;
        _cacheFor = cacheFor;
        _logger = logger;
    }

    public async Task<IReadOnlyList<InstagramPostDto>> GetRecentAsync(int count, CancellationToken cancellationToken = default)
    {
        if (count <= 0)
            return [];

        if (DateTimeOffset.UtcNow < _nextRefresh)
            return Take(count);

        // One caller refreshes; everyone else waits and then reads the result. Without this, the
        // first request after the cache expires would send as many identical Graph calls as there
        // happen to be visitors on the page at that moment.
        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            if (DateTimeOffset.UtcNow < _nextRefresh)
                return Take(count);

            await RefreshAsync(cancellationToken);
        }
        finally
        {
            _refreshLock.Release();
        }

        return Take(count);
    }

    private IReadOnlyList<InstagramPostDto> Take(int count)
        => _cached.Count <= count ? _cached : _cached.Take(count).ToList();

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        var url = $"{_apiBaseUrl}/{_apiVersion}/{_userId}/media" +
                  $"?fields={Fields}&limit={_fetchCount}&access_token={Uri.EscapeDataString(_accessToken)}";

        try
        {
            using var response = await _http.GetAsync(url, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                // The body carries Meta's own explanation — an expired token, an account that is no
                // longer Business/Creator, a permission that lapsed at review. Logging it is the
                // difference between "the gallery is empty" and knowing why, which is the same
                // lesson the Mailgun 404 taught us.
                _logger.LogError(
                    "Instagram feed request failed with {StatusCode}. Graph API said: {Body}",
                    (int)response.StatusCode, Truncate(body, 1000));
                _nextRefresh = DateTimeOffset.UtcNow + RetryAfterFailure;
                return;
            }

            var posts = Parse(body);
            if (posts.Count == 0)
                _logger.LogWarning("Instagram feed returned no usable posts. The account may have none, or none with an image.");

            _cached = posts;
            _nextRefresh = DateTimeOffset.UtcNow + _cacheFor;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Whatever went wrong, the home page still has to render. The previously cached posts
            // are kept rather than cleared: yesterday's gallery beats an empty row.
            _logger.LogError(ex, "Instagram feed could not be refreshed. Serving {Count} cached post(s).", _cached.Count);
            _nextRefresh = DateTimeOffset.UtcNow + RetryAfterFailure;
        }
    }

    private static List<InstagramPostDto> Parse(string body)
    {
        var posts = new List<InstagramPostDto>();

        using var document = JsonDocument.Parse(body);
        if (!document.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
            return posts;

        foreach (var item in data.EnumerateArray())
        {
            var mediaType = String(item, "media_type");

            // A video or reel's media_url is the video file itself, which cannot go into an <img>;
            // thumbnail_url is its poster frame. An album reports the first child's image in
            // media_url, which is the right one to show for a carousel post.
            var imageUrl = mediaType is "VIDEO" or "REELS"
                ? String(item, "thumbnail_url") ?? String(item, "media_url")
                : String(item, "media_url");

            var permalink = String(item, "permalink");
            if (string.IsNullOrWhiteSpace(imageUrl) || string.IsNullOrWhiteSpace(permalink))
                continue;

            posts.Add(new InstagramPostDto
            {
                Id = String(item, "id") ?? permalink,
                ImageUrl = imageUrl,
                Permalink = permalink,
                Caption = String(item, "caption"),
                PostedAt = DateTimeOffset.TryParse(String(item, "timestamp"), out var timestamp) ? timestamp : null
            });
        }

        return posts;
    }

    private static string? String(JsonElement element, string property)
        => element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..max] + "...";
}
