namespace MiskBeirut.Application.Services;

/// <summary>
/// Writes an uploaded CV into permanent private storage (never web-accessible directly). The file's
/// extension, declared content type, and actual byte signature are verified by the Web layer's
/// FileTypeValidator before it ever reaches here.
/// </summary>
public interface ICvSubmissionService
{
    /// <param name="desiredBaseName">
    /// Preferred file name (e.g. the applicant's name), sanitized for the filesystem and
    /// de-duplicated (mahmoud.pdf, mahmoud-2.pdf, ...) if it collides with an existing file.
    /// </param>
    /// <returns>The stored file name. Not a public URL — a private storage reference.</returns>
    Task<string> SubmitAsync(Stream content, string originalFileName, string desiredBaseName, CancellationToken cancellationToken = default);

    /// <summary>Removes a previously stored CV from disk. No-op if the file is already gone.</summary>
    Task DeleteAsync(string storedFileName, CancellationToken cancellationToken = default);

    /// <summary>Opens a previously stored CV for reading (Cms review only — never a public URL). Null if the file is missing.</summary>
    Task<Stream?> OpenReadAsync(string storedFileName, CancellationToken cancellationToken = default);
}
