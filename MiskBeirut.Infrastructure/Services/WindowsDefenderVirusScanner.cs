using System.Diagnostics;
using Microsoft.Extensions.Logging;
using MiskBeirut.Application.Services;

namespace MiskBeirut.Infrastructure.Services;

/// <summary>
/// Scans a file with Windows Defender's command-line scanner (MpCmdRun.exe). Fails closed: if the
/// scanner can't be found or run, the file is treated as unscanned rather than assumed safe — every
/// caller (CV submissions, Cms uploads) rejects on <see cref="VirusScanOutcome.ScanUnavailable"/>
/// the same as it would on <see cref="VirusScanOutcome.Infected"/>.
/// </summary>
public class WindowsDefenderVirusScanner : IVirusScanner
{
    private readonly string _mpCmdRunPath;
    private readonly ILogger<WindowsDefenderVirusScanner> _logger;

    public WindowsDefenderVirusScanner(string mpCmdRunPath, ILogger<WindowsDefenderVirusScanner> logger)
    {
        _mpCmdRunPath = mpCmdRunPath;
        _logger = logger;
    }

    public async Task<VirusScanOutcome> ScanAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_mpCmdRunPath))
        {
            _logger.LogError("MpCmdRun.exe not found at {Path}. Scan cannot run.", _mpCmdRunPath);
            return VirusScanOutcome.ScanUnavailable;
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _mpCmdRunPath,
                Arguments = $"-Scan -ScanType 3 -DisableRemediation -File \"{filePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                _logger.LogError("Failed to start MpCmdRun.exe for {File}.", filePath);
                return VirusScanOutcome.ScanUnavailable;
            }

            var stdOut = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stdErr = await process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            _logger.LogInformation("Defender scan of {File} exited {Code}. Output: {Output} {Error}",
                filePath, process.ExitCode, stdOut, stdErr);

            // Documented/observed MpCmdRun exit codes: 0 = clean, 2 = threat(s) found.
            // Anything else (missing signatures, engine error, etc.) is treated as "couldn't verify" and rejected.
            return process.ExitCode switch
            {
                0 => VirusScanOutcome.Clean,
                2 => VirusScanOutcome.Infected,
                _ => VirusScanOutcome.ScanUnavailable
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running virus scan for {File}.", filePath);
            return VirusScanOutcome.ScanUnavailable;
        }
    }

    public static string FindDefaultMpCmdRunPath()
    {
        const string fixedPath = @"C:\Program Files\Windows Defender\MpCmdRun.exe";
        if (File.Exists(fixedPath))
            return fixedPath;

        var platformRoot = @"C:\ProgramData\Microsoft\Windows Defender\Platform";
        if (Directory.Exists(platformRoot))
        {
            var newest = Directory.GetDirectories(platformRoot)
                .OrderByDescending(d => d)
                .FirstOrDefault();
            if (newest is not null)
            {
                var candidate = Path.Combine(newest, "MpCmdRun.exe");
                if (File.Exists(candidate))
                    return candidate;
            }
        }

        return fixedPath; // Not found; ScanAsync will log and reject rather than silently skip scanning.
    }
}
