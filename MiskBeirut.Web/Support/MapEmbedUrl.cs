using System.Text.RegularExpressions;

namespace MiskBeirut.Web.Support;

/// <summary>
/// Turns a Google Maps link into one an &lt;iframe&gt; will actually display.
///
/// This is why the Cms map field "didn't work": Google serves /maps/place/... and /maps/search/...
/// pages with X-Frame-Options: SAMEORIGIN, so pasting the link from the browser's address bar (or
/// the Share dialog) into the embed field produced a blank rectangle with nothing to explain it.
/// Only two URL shapes are frameable — the /maps/embed?pb=... one behind Share → Embed a map, and
/// the query form maps.google.com/maps?q=...&amp;output=embed — so anything else is converted to the
/// second, and a link that can't be converted is refused at save time instead of silently blanking
/// the map on the live site.
/// </summary>
public static partial class MapEmbedUrl
{
    /// <summary>
    /// The place's own pin in a /maps/place/... url: the "!3d33.87!4d35.48" pair. Distinct from the
    /// "@..." coordinates below, which are only where the map was centred when the link was copied.
    /// </summary>
    [GeneratedRegex(@"!3d(-?\d+\.\d+)!4d(-?\d+\.\d+)", RegexOptions.CultureInvariant)]
    private static partial Regex PlacePinPattern();

    /// <summary>Coordinates in a /maps/place/... or /maps/@... url: the "@33.89,35.51,17z" part.</summary>
    [GeneratedRegex(@"@(-?\d+\.\d+),(-?\d+\.\d+)(?:,(\d+(?:\.\d+)?)z)?", RegexOptions.CultureInvariant)]
    private static partial Regex CoordinatesPattern();

    /// <summary>The place or search term in a /maps/place/NAME or /maps/search/NAME url.</summary>
    [GeneratedRegex(@"/maps/(?:place|search|dir)/([^/@?]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PlacePattern();

    /// <summary>A whole &lt;iframe src="..."&gt; snippet, as copied from Google's "Embed a map" tab.</summary>
    [GeneratedRegex("<iframe[^>]*\\ssrc=[\"']([^\"']+)[\"']", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex IframeSrcPattern();

    /// <summary>True for the attribute names whose values are iframe sources rather than plain links.</summary>
    public static bool IsMapEmbedAttribute(string attributeName)
        => attributeName.EndsWith("map_embed_url", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The embeddable form of <paramref name="value"/>, or null if it isn't a Google Maps link this
    /// can convert (a maps.app.goo.gl short link, for one — resolving that needs a network call
    /// Google may answer with a redirect chain, so the editor is asked for the full link instead).
    /// A blank input returns blank: clearing the field is allowed.
    /// </summary>
    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        var url = value.Trim();

        // Someone pasted the whole <iframe ...> snippet from Google's "Embed a map" tab rather than
        // just its src. Take the src — it's what they meant, and the alternative is an error over
        // something that already contains exactly the right URL.
        var iframeSrc = IframeSrcPattern().Match(url);
        if (iframeSrc.Success)
            url = iframeSrc.Groups[1].Value.Trim();

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            return null;

        // Already embeddable, either shape.
        if (uri.AbsolutePath.Contains("/maps/embed", StringComparison.OrdinalIgnoreCase) ||
            uri.Query.Contains("output=embed", StringComparison.OrdinalIgnoreCase))
            return url;

        if (!uri.Host.EndsWith("google.com", StringComparison.OrdinalIgnoreCase) &&
            !uri.Host.EndsWith("google.com.lb", StringComparison.OrdinalIgnoreCase))
            return null;

        // Coordinates are the most faithful conversion — they point at a fixed spot rather than at
        // whatever Google decides the place name means today. But a /maps/place/ link carries two
        // different pairs, and they are not interchangeable: "@lat,lng,17z" is where the map happened
        // to be centred when the link was copied, while "!3d lat !4d lng" is the establishment's own
        // pin. For Misk Beirut the two sit roughly 250 m apart, so taking the first put the marker in
        // the wrong block. Prefer the pin, falling back to the viewport only when there isn't one —
        // a bare /maps/@... link with no place attached.
        var viewport = CoordinatesPattern().Match(url);
        var pin = PlacePinPattern().Match(url);
        if (pin.Success)
        {
            var pinZoom = viewport.Success && viewport.Groups[3].Success ? viewport.Groups[3].Value.Split('.')[0] : "17";
            return $"https://maps.google.com/maps?q={pin.Groups[1].Value},{pin.Groups[2].Value}&z={pinZoom}&output=embed";
        }

        var coordinates = viewport;
        if (coordinates.Success)
        {
            var zoom = coordinates.Groups[3].Success ? coordinates.Groups[3].Value.Split('.')[0] : "16";
            return $"https://maps.google.com/maps?q={coordinates.Groups[1].Value},{coordinates.Groups[2].Value}&z={zoom}&output=embed";
        }

        var place = PlacePattern().Match(uri.AbsolutePath);
        if (place.Success)
            return $"https://maps.google.com/maps?q={place.Groups[1].Value}&output=embed";

        // /maps?q=... or /maps/search/?api=1&query=... — reuse whichever term it carries.
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);
        foreach (var key in new[] { "q", "query", "daddr" })
        {
            if (query.TryGetValue(key, out var values) && !string.IsNullOrWhiteSpace(values.FirstOrDefault()))
                return $"https://maps.google.com/maps?q={Uri.EscapeDataString(values.First()!)}&output=embed";
        }

        return null;
    }

    /// <summary>The message shown when <see cref="Normalize"/> can't convert what was pasted.</summary>
    public const string RejectionMessage =
        "That doesn't look like a Google Maps link this can embed. Open the place on Google Maps, " +
        "copy the URL from the browser's address bar (it starts https://www.google.com/maps/place/...), " +
        "and paste that. A shortened maps.app.goo.gl link won't work — open it first, then copy the full URL.";
}
