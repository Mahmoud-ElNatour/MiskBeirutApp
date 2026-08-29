namespace MiskBeirut.Application.Services;

public enum CvSubmissionOutcome
{
    Accepted,
    Infected,
    ScanUnavailable
}

public sealed record CvSubmissionResult
{
    public required CvSubmissionOutcome Outcome { get; init; }

    /// <summary>Set only when <see cref="Outcome"/> is <see cref="CvSubmissionOutcome.Accepted"/>. Not a public URL — a private storage reference.</summary>
    public string? StoredFileName { get; init; }
}

/// <summary>
/// Writes an uploaded CV to a temporary location, scans it for viruses, and — only if clean —
/// moves it into permanent private storage (never web-accessible directly).
/// </summary>
public interface ICvSubmissionService
{
    /// <param name="desiredBaseName">
    /// Preferred file name (e.g. the applicant's name), sanitized for the filesystem and
    /// de-duplicated (mahmoud.pdf, mahmoud-2.pdf, ...) if it collides with an existing file.
    /// </param>
    Task<CvSubmissionResult> SubmitAsync(Stream content, string originalFileName, string desiredBaseName, CancellationToken cancellationToken = default);

    /// <summary>Removes a previously stored CV from disk. No-op if the file is already gone.</summary>
    Task DeleteAsync(string storedFileName, CancellationToken cancellationToken = default);

    /// <summary>Opens a previously stored CV for reading (Cms review only — never a public URL). Null if the file is missing.</summary>
    Task<Stream?> OpenReadAsync(string storedFileName, CancellationToken cancellationToken = default);
}
