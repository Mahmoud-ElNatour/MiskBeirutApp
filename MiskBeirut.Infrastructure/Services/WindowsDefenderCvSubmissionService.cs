using MiskBeirut.Application.Services;

namespace MiskBeirut.Infrastructure.Services;

/// <summary>
/// Writes an uploaded CV to a temp file, scans it via <see cref="IVirusScanner"/>, and only moves
/// it into permanent private storage if clean. Fails closed: a scan that couldn't run is treated
/// the same as an infected file — see <see cref="WindowsDefenderVirusScanner"/>.
/// </summary>
public class WindowsDefenderCvSubmissionService : ICvSubmissionService
{
    private readonly IVirusScanner _scanner;
    private readonly string _tempDirectory;
    private readonly string _storageDirectory;

    public WindowsDefenderCvSubmissionService(IVirusScanner scanner, string tempDirectory, string storageDirectory)
    {
        _scanner = scanner;
        _tempDirectory = tempDirectory;
        _storageDirectory = storageDirectory;

        Directory.CreateDirectory(_tempDirectory);
        Directory.CreateDirectory(_storageDirectory);
    }

    public async Task<CvSubmissionResult> SubmitAsync(Stream content, string originalFileName, string desiredBaseName, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(originalFileName);
        var tempPath = Path.Combine(_tempDirectory, $"{Guid.NewGuid():N}{extension}");

        await using (var tempFile = File.Create(tempPath))
        {
            await content.CopyToAsync(tempFile, cancellationToken);
        }

        try
        {
            var scanOutcome = await _scanner.ScanAsync(tempPath, cancellationToken);
            if (scanOutcome != VirusScanOutcome.Clean)
            {
                var outcome = scanOutcome == VirusScanOutcome.Infected
                    ? CvSubmissionOutcome.Infected
                    : CvSubmissionOutcome.ScanUnavailable;
                return new CvSubmissionResult { Outcome = outcome };
            }

            var storedFileName = MoveToStorage(tempPath, Sanitize(desiredBaseName), extension);
            return new CvSubmissionResult { Outcome = CvSubmissionOutcome.Accepted, StoredFileName = storedFileName };
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    /// <summary>
    /// Moves the scanned temp file into storage as "{baseName}{extension}", falling back to
    /// "{baseName}-2{extension}", "-3", ... if that name is already taken (same applicant name,
    /// or a repeat application) — never overwrites an existing CV.
    /// </summary>
    private string MoveToStorage(string tempPath, string baseName, string extension)
    {
        var attempt = 1;
        while (true)
        {
            var candidateName = attempt == 1 ? $"{baseName}{extension}" : $"{baseName}-{attempt}{extension}";
            var candidatePath = Path.Combine(_storageDirectory, candidateName);
            try
            {
                File.Move(tempPath, candidatePath);
                return candidateName;
            }
            catch (IOException) when (File.Exists(candidatePath))
            {
                attempt++;
            }
        }
    }

    private static string Sanitize(string name)
    {
        var sb = new System.Text.StringBuilder();
        var lastWasSeparator = false;
        foreach (var c in name.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(c);
                lastWasSeparator = false;
            }
            else if (!lastWasSeparator && sb.Length > 0)
            {
                sb.Append('-');
                lastWasSeparator = true;
            }
        }

        var result = sb.ToString().TrimEnd('-');
        return string.IsNullOrEmpty(result) ? "applicant" : result;
    }

    public Task DeleteAsync(string storedFileName, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(_storageDirectory, storedFileName);
        if (File.Exists(path))
            File.Delete(path);

        return Task.CompletedTask;
    }

    public Task<Stream?> OpenReadAsync(string storedFileName, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(_storageDirectory, storedFileName);
        if (!File.Exists(path))
            return Task.FromResult<Stream?>(null);

        Stream stream = File.OpenRead(path);
        return Task.FromResult<Stream?>(stream);
    }
}
