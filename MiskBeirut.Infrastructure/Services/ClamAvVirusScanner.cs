using System.Diagnostics;
using Microsoft.Extensions.Logging;
using MiskBeirut.Application.Services;

namespace MiskBeirut.Infrastructure.Services;

/// <summary>
/// Scans a file with ClamAV's command-line scanner (clamscan.exe — the standalone scanner, not the
/// clamd-daemon client, so there's no background service to keep running across app pool recycles).
/// Exists as the fallback engine when Windows Defender isn't installed on the host — see
/// <see cref="FallbackVirusScanner"/>. Fails closed like <see cref="WindowsDefenderVirusScanner"/>:
/// if the executable can't be found or run, the file is treated as unscanned.
/// </summary>
public class ClamAvVirusScanner : IVirusScanner
{
    private readonly string _clamScanPath;
    private readonly ILogger<ClamAvVirusScanner> _logger;

    public ClamAvVirusScanner(string clamScanPath, ILogger<ClamAvVirusScanner> logger)
    {
        _clamScanPath = clamScanPath;
        _logger = logger;
    }

    public async Task<VirusScanOutcome> ScanAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_clamScanPath))
        {
            _logger.LogError("clamscan.exe not found at {Path}. ClamAV scan cannot run.", _clamScanPath);
            return VirusScanOutcome.ScanUnavailable;
        }

        try
        {
            // ClamAvBootstrapService (if it did the install) always writes the database next to
            // clamscan.exe in a "database" subfolder — pointing at it explicitly removes any
            // ambiguity about where clamscan looks by default. If that folder doesn't exist (a
            // manual install with the database placed elsewhere), fall back to clamscan's own
            // default search behavior instead of pointing it at a folder that isn't there.
            var databaseDirectory = Path.Combine(Path.GetDirectoryName(_clamScanPath)!, "database");
            var databaseArg = Directory.Exists(databaseDirectory) ? $"--database=\"{databaseDirectory}\" " : "";

            var startInfo = new ProcessStartInfo
            {
                FileName = _clamScanPath,
                Arguments = $"{databaseArg}--no-summary \"{filePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                _logger.LogError("Failed to start clamscan.exe for {File}.", filePath);
                return VirusScanOutcome.ScanUnavailable;
            }

            var stdOut = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stdErr = await process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            _logger.LogInformation("ClamAV scan of {File} exited {Code}. Output: {Output} {Error}",
                filePath, process.ExitCode, stdOut, stdErr);

            // Documented clamscan exit codes: 0 = no virus found, 1 = virus(es) found, 2 = an error occurred
            // (bad options, file access, corrupt/missing virus database, etc.) — treated as "couldn't verify".
            return process.ExitCode switch
            {
                0 => VirusScanOutcome.Clean,
                1 => VirusScanOutcome.Infected,
                _ => VirusScanOutcome.ScanUnavailable
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running ClamAV scan for {File}.", filePath);
            return VirusScanOutcome.ScanUnavailable;
        }
    }

    public static string FindDefaultClamAvPath()
    {
        string[] candidates =
        [
            @"C:\Program Files\ClamAV\clamscan.exe",
            @"C:\Program Files (x86)\ClamAV\clamscan.exe"
        ];

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
                return candidate;
        }

        return candidates[0]; // Not found; ScanAsync will log and report unavailable rather than silently skip scanning.
    }
}
