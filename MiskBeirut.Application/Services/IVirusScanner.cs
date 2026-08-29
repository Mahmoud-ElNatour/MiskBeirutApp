namespace MiskBeirut.Application.Services;

public enum VirusScanOutcome
{
    Clean,
    Infected,
    ScanUnavailable
}

/// <summary>
/// Scans a file already written to disk for malware — the shared primitive behind every upload
/// path that accepts files from the public internet (Careers CV submissions, Cms image/menu-PDF
/// uploads) before that file is moved anywhere permanent or made web-accessible.
/// </summary>
public interface IVirusScanner
{
    Task<VirusScanOutcome> ScanAsync(string filePath, CancellationToken cancellationToken = default);
}
