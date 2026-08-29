using System.Text;

namespace MiskBeirut.Web.Support;

/// <summary>
/// Validates an uploaded file's extension, declared Content-Type, AND actual byte signature before
/// any upload endpoint writes it to disk. Extension and Content-Type are both attacker-controlled —
/// a renamed file still carries some Content-Type, and a hand-crafted request can set the header to
/// anything — so neither proves what the file actually is. The signature check reads the file's own
/// bytes (its "magic number") and catches a payload that got the first two checks to line up but
/// isn't really a PDF/image at all.
/// </summary>
public static class FileTypeValidator
{
    /// <summary>PDF documents: CVs, the public menu.</summary>
    public static readonly string[] PdfExtensions = [".pdf"];
    public static readonly string[] PdfContentTypes = ["application/pdf"];

    /// <summary>Images accepted for CMS content (page images, gallery).</summary>
    public static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif", ".svg"];
    public static readonly string[] ImageContentTypes =
    [
        "image/jpeg", "image/png", "image/webp", "image/gif", "image/svg+xml"
    ];

    /// <summary>Bytes sniffed for a binary format's magic number — every signature below fits well within this.</summary>
    private const int SignatureSniffBytes = 16;

    /// <summary>
    /// SVG has no fixed binary signature (it's XML text) — an optional BOM, XML declaration, DOCTYPE,
    /// and comments can all precede the actual &lt;svg&gt; tag, so this sniffs a larger text window
    /// instead of a fixed byte prefix.
    /// </summary>
    private const int SvgSniffBytes = 4096;

    /// <summary>
    /// Returns null if <paramref name="file"/>'s extension, Content-Type, and actual byte signature
    /// all check out against the given allow-lists; otherwise a user-facing message naming
    /// <paramref name="kindLabel"/> (e.g. "CV", "Image") for the caller to return as-is.
    /// </summary>
    public static async Task<string?> ValidateAsync(
        IFormFile file,
        string kindLabel,
        IReadOnlyCollection<string> allowedExtensions,
        IReadOnlyCollection<string> allowedContentTypes,
        CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            return $"{kindLabel} must be one of: {string.Join(", ", allowedExtensions)}.";

        if (string.IsNullOrEmpty(file.ContentType) || !allowedContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
            return $"{kindLabel} file type could not be verified. Please upload a valid {kindLabel.ToLowerInvariant()} file.";

        if (!await MatchesSignatureAsync(file, extension, cancellationToken))
            return $"{kindLabel} file doesn't look like a real {extension.TrimStart('.').ToUpperInvariant()} — its content doesn't match its extension. It may be corrupted, mislabeled, or not the file type it claims to be.";

        return null;
    }

    /// <summary>Reads the file's own leading bytes and checks them against the known signature for its extension.</summary>
    private static async Task<bool> MatchesSignatureAsync(IFormFile file, string extension, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();

        if (string.Equals(extension, ".svg", StringComparison.OrdinalIgnoreCase))
        {
            var textBuffer = new byte[SvgSniffBytes];
            var textRead = await ReadFullyAsync(stream, textBuffer, cancellationToken);
            var text = Encoding.UTF8.GetString(textBuffer, 0, textRead);
            return text.Contains("<svg", StringComparison.OrdinalIgnoreCase);
        }

        var header = new byte[SignatureSniffBytes];
        var headerRead = await ReadFullyAsync(stream, header, cancellationToken);

        return extension.ToLowerInvariant() switch
        {
            ".pdf" => StartsWith(header, headerRead, 0x25, 0x50, 0x44, 0x46, 0x2D),                         // %PDF-
            ".jpg" or ".jpeg" => StartsWith(header, headerRead, 0xFF, 0xD8, 0xFF),                          // JFIF/Exif SOI marker
            ".png" => StartsWith(header, headerRead, 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A),       // \x89PNG\r\n\x1a\n
            ".gif" => StartsWith(header, headerRead, 0x47, 0x49, 0x46, 0x38) && headerRead >= 6            // GIF8
                && (header[4] == 0x37 || header[4] == 0x39) && header[5] == 0x61,                            // "7a" or "9a"
            ".webp" => headerRead >= 12 && StartsWith(header, headerRead, 0x52, 0x49, 0x46, 0x46)           // "RIFF"
                && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50,       // "WEBP"
            _ => true // no known binary signature for this extension — the extension/Content-Type checks already ran
        };
    }

    private static bool StartsWith(byte[] buffer, int bytesRead, params byte[] signature) =>
        bytesRead >= signature.Length && signature.AsSpan().SequenceEqual(buffer.AsSpan(0, signature.Length));

    /// <summary>
    /// A single stream.ReadAsync call is free to return fewer bytes than requested even mid-stream —
    /// this keeps reading until the buffer is full or the stream ends, so a short first read can't be
    /// mistaken for a too-small (and therefore suspicious) file.
    /// </summary>
    private static async Task<int> ReadFullyAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        var totalRead = 0;
        int read;
        while (totalRead < buffer.Length &&
               (read = await stream.ReadAsync(buffer.AsMemory(totalRead, buffer.Length - totalRead), cancellationToken)) > 0)
        {
            totalRead += read;
        }
        return totalRead;
    }
}
