namespace MiskBeirut.Application.Dtos.Social;

/// <summary>One post from the restaurant's Instagram account, as the public gallery shows it.</summary>
public sealed record InstagramPostDto
{
    public string Id { get; init; } = null!;

    /// <summary>
    /// The image to display. For a video or a reel this is the poster frame rather than the video
    /// itself — the gallery is a row of square photographs, and an inline video in each tile would
    /// be a different section.
    /// </summary>
    public string ImageUrl { get; init; } = null!;

    /// <summary>The post's page on instagram.com — where the tile links to.</summary>
    public string Permalink { get; init; } = null!;

    /// <summary>The caption as written, or null. Used for the tile's alt text, trimmed to a sensible length.</summary>
    public string? Caption { get; init; }

    public DateTimeOffset? PostedAt { get; init; }

    /// <summary>
    /// Alt text for the tile: the caption's first line, capped so a long caption doesn't become a
    /// paragraph read out by a screen reader. Falls back to naming the account, which is more useful
    /// than an empty alt on an image that is genuinely content.
    /// </summary>
    public string AltText(string accountName)
    {
        if (string.IsNullOrWhiteSpace(Caption))
            return $"Instagram post from {accountName}";

        var firstLine = Caption.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault() ?? "";

        return firstLine.Length <= 120 ? firstLine : firstLine[..117].TrimEnd() + "...";
    }
}
