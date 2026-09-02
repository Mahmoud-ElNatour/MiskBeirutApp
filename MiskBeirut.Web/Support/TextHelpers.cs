namespace MiskBeirut.Web.Support;

public static class TextHelpers
{
    /// <summary>
    /// Shortens a value for a table cell, cutting at the last word boundary before the limit so a
    /// word isn't sliced in half, and appending an ellipsis. Anything already short enough comes
    /// back untouched, and null/blank becomes an em dash — the placeholder these tables use for
    /// "nothing recorded".
    /// </summary>
    public static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "—";

        var text = value.Trim();

        // A long message is often several lines; collapsed to one, it reads as a preview rather than
        // dragging the row's height out with the line breaks it happens to contain.
        text = string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        if (text.Length <= maxLength)
            return text;

        var cut = text.LastIndexOf(' ', Math.Min(maxLength, text.Length - 1));
        if (cut < maxLength / 2)
            cut = maxLength;

        return text[..cut].TrimEnd() + "…";
    }
}
