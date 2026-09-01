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

            // MpCmdRun exits 0 when the scan ran and found nothing, and 2 when it found threats —
            // but it ALSO exits 2 when the scan could not run at all (Defender disabled or displaced
            // by third-party AV, the service refusing the app pool identity, signatures missing).
            // Trusting the code alone therefore reports a perfectly good CV as infected on any host
            // where Defender can't run, which is exactly what was happening on the live site. A real
            // detection always names what it found, so exit 2 only counts as Infected when the
            // scanner's own output says a threat was found; otherwise it's "couldn't verify", which
            // lets FallbackVirusScanner move on to ClamAV instead of rejecting the applicant.
            if (process.ExitCode == 0)
                return VirusScanOutcome.Clean;

            if (process.ExitCode == 2 && ReportsThreatFound(stdOut))
                return VirusScanOutcome.Infected;

            _logger.LogWarning("Defender could not verify {File} (exit {Code}). Treating as unscanned rather than infected.", filePath, process.ExitCode);
            return VirusScanOutcome.ScanUnavailable;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running virus scan for {File}.", filePath);
            return VirusScanOutcome.ScanUnavailable;
        }
    }

    /// <summary>
    /// True when MpCmdRun's scan output actually names a detection. "found no threats" contains the
    /// word "threat", so the negative phrasings are ruled out before the positive ones are checked.
    /// </summary>
    private static bool ReportsThreatFound(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
            return false;

        if (output.Contains("found no threats", StringComparison.OrdinalIgnoreCase) ||
            output.Contains("no threats detected", StringComparison.OrdinalIgnoreCase))
            return false;

        return output.Contains("threat(s) found", StringComparison.OrdinalIgnoreCase)
            || output.Contains("threats found", StringComparison.OrdinalIgnoreCase)
            || output.Contains("found threat", StringComparison.OrdinalIgnoreCase)
            || output.Contains("list of detected threats", StringComparison.OrdinalIgnoreCase);
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
