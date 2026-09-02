using MiskBeirut.Application.Services;

namespace MiskBeirut.Infrastructure.Services;

/// <summary>
/// Writes an uploaded CV to a temp file and then moves it into permanent private storage. The temp
/// hop keeps a half-received upload out of the storage folder — a connection that drops mid-copy
/// leaves a stray temp file that gets deleted, never a truncated CV under an applicant's name.
/// </summary>
public class FileSystemCvSubmissionService : ICvSubmissionService
{
    private readonly string _tempDirectory;
    private readonly string _storageDirectory;

    public FileSystemCvSubmissionService(string tempDirectory, string storageDirectory)
    {
        _tempDirectory = tempDirectory;
        _storageDirectory = storageDirectory;

        Directory.CreateDirectory(_tempDirectory);
        Directory.CreateDirectory(_storageDirectory);
    }

    public async Task<string> SubmitAsync(Stream content, string originalFileName, string desiredBaseName, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(originalFileName);
        var tempPath = Path.Combine(_tempDirectory, $"{Guid.NewGuid():N}{extension}");

        await using (var tempFile = File.Create(tempPath))
        {
            await content.CopyToAsync(tempFile, cancellationToken);
        }

        try
        {
            return MoveToStorage(tempPath, Sanitize(desiredBaseName), extension);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    /// <summary>
    /// Moves the temp file into storage as "{baseName}{extension}", falling back to
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
