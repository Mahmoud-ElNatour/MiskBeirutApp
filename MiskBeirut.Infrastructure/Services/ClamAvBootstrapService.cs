using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MiskBeirut.Infrastructure.Services;

/// <summary>
/// On app startup, if ClamAV isn't already present at one of the well-known install locations
/// (see <see cref="ClamAvVirusScanner.FindDefaultClamAvPath"/>), downloads the current release's
/// Windows MSI from ClamAV's GitHub releases, installs it silently, writes a minimal
/// <c>freshclam.conf</c> (the official Windows package ships without one — see
/// docs.clamav.net/manual/Installing.html), and runs freshclam once to fetch an initial virus
/// database. Without that last step clamscan has nothing to scan against and every scan reports
/// <see cref="MiskBeirut.Application.Services.VirusScanOutcome.ScanUnavailable"/>.
///
/// Runs as a fire-and-forget background task, not blocking app startup — the install plus database
/// download can take several minutes, and every upload endpoint already handles "no scanner
/// available yet" by rejecting with "try again shortly" (<see cref="FallbackVirusScanner"/>), so
/// there's nothing worth blocking startup on.
///
/// <b>Requires the app pool identity to have permission to run an elevated installer.</b> On
/// locked-down shared hosting this will very likely fail closed — the failure is logged clearly and
/// left there; ClamAV simply stays unavailable and Windows Defender (if present) keeps covering
/// uploads on its own via <see cref="FallbackVirusScanner"/>. This is a best-effort convenience, not
/// something to depend on for a permission tier you don't control.
/// </summary>
public class ClamAvBootstrapService : IHostedService
{
    private const string LatestReleaseApiUrl = "https://api.github.com/repos/Cisco-Talos/clamav/releases/latest";

    /// <summary>Official CVD mirror hostname every ClamAV install (Linux, macOS, Windows) points freshclam at by default.</summary>
    private const string DefaultDatabaseMirror = "database.clamav.net";

    /// <summary>
    /// How long to wait before retrying after a failed install. Without this, a host where the
    /// install can never succeed (no permission to run installers — the likely case on locked-down
    /// shared hosting) would re-download a ~200 MB MSI on every single app start, and IIS recycles
    /// app pools routinely. The marker file makes the retry periodic instead of per-recycle.
    /// </summary>
    private static readonly TimeSpan RetryAfterFailure = TimeSpan.FromHours(24);

    private readonly bool _enabled;
    private readonly string _configuredClamScanPath;
    private readonly string _stateDirectory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<ClamAvBootstrapService> _logger;

    /// <param name="configuredClamScanPath">
    /// The same clamscan.exe path <see cref="ClamAvVirusScanner"/> was registered with, so an
    /// operator who installed ClamAV somewhere non-default and pointed VirusScanning:ClamAvPath at
    /// it is detected as "already installed" rather than having a second copy installed over the top.
    /// </param>
    /// <param name="stateDirectory">Where the failed-attempt marker lives (App_Data).</param>
    public ClamAvBootstrapService(
        bool enabled,
        string configuredClamScanPath,
        string stateDirectory,
        IHttpClientFactory httpClientFactory,
        IHostApplicationLifetime lifetime,
        ILogger<ClamAvBootstrapService> logger)
    {
        _enabled = enabled;
        _configuredClamScanPath = configuredClamScanPath;
        _stateDirectory = stateDirectory;
        _httpClientFactory = httpClientFactory;
        _lifetime = lifetime;
        _logger = logger;
    }

    private string FailureMarkerPath => Path.Combine(_stateDirectory, "clamav-install-failed.marker");

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_enabled)
        {
            _logger.LogInformation("ClamAV auto-install is disabled (VirusScanning:AutoInstallClamAv=false).");
            return Task.CompletedTask;
        }

        if (File.Exists(_configuredClamScanPath) || File.Exists(ClamAvVirusScanner.FindDefaultClamAvPath()))
        {
            _logger.LogInformation("ClamAV already present; skipping auto-install.");
            return Task.CompletedTask;
        }

        if (RecentlyFailed(out var lastAttempt))
        {
            _logger.LogWarning(
                "Skipping ClamAV auto-install: the last attempt failed at {LastAttempt} and retries are throttled to once every {Hours}h. " +
                "Delete {Marker} to force a retry sooner.",
                lastAttempt, RetryAfterFailure.TotalHours, FailureMarkerPath);
            return Task.CompletedTask;
        }

        // Fire-and-forget with the app's own shutdown token, not the token StartAsync received —
        // that one is only valid for the startup window, and this can legitimately outlive it by
        // several minutes (MSI download/install + virus database fetch).
        _ = RunAsync(_lifetime.ApplicationStopping);
        return Task.CompletedTask;
    }

    private bool RecentlyFailed(out DateTimeOffset lastAttempt)
    {
        lastAttempt = default;
        try
        {
            if (!File.Exists(FailureMarkerPath))
                return false;

            lastAttempt = File.GetLastWriteTimeUtc(FailureMarkerPath);
            return DateTimeOffset.UtcNow - lastAttempt < RetryAfterFailure;
        }
        catch (Exception ex)
        {
            // An unreadable marker shouldn't block a legitimate install attempt.
            _logger.LogWarning(ex, "Could not read the ClamAV install failure marker; proceeding as if none exists.");
            return false;
        }
    }

    private void RecordFailedAttempt()
    {
        try
        {
            Directory.CreateDirectory(_stateDirectory);
            File.WriteAllText(FailureMarkerPath,
                $"Last failed ClamAV auto-install attempt: {DateTimeOffset.UtcNow:O}{Environment.NewLine}" +
                $"Delete this file to allow an immediate retry.{Environment.NewLine}");
        }
        catch (Exception ex)
        {
            // Best effort — worst case the next start retries, which is the old behaviour.
            _logger.LogWarning(ex, "Could not write the ClamAV install failure marker.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        string? msiPath = null;
        var succeeded = false;
        var interrupted = false;
        try
        {
            _logger.LogInformation("ClamAV not found — starting first-run install.");

            msiPath = await DownloadLatestMsiAsync(cancellationToken);
            if (msiPath is null)
                return;

            if (!await RunMsiInstallAsync(msiPath, cancellationToken))
                return;

            var clamScanPath = ClamAvVirusScanner.FindDefaultClamAvPath();
            if (!File.Exists(clamScanPath))
            {
                _logger.LogError(
                    "msiexec reported success but clamscan.exe still isn't at any known default location. " +
                    "The install may have gone somewhere non-standard — ClamAV auto-install can't verify or use it. " +
                    "If it did install elsewhere, point VirusScanning:ClamAvPath at it so this stops retrying.");
                return;
            }

            await SetUpVirusDatabaseAsync(clamScanPath, cancellationToken);
            succeeded = true;
            _logger.LogInformation("ClamAV first-run install complete ({Path}).", clamScanPath);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            interrupted = true;
            _logger.LogWarning("ClamAV auto-install was interrupted by app shutdown.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "ClamAV auto-install failed. Uploads will keep falling back to whatever else is configured " +
                "(Windows Defender, or outright rejection) until this is resolved — either by fixing whatever " +
                "failed here, or installing ClamAV manually.");
        }
        finally
        {
            if (msiPath is not null && File.Exists(msiPath))
                File.Delete(msiPath);

            // A shutdown interruption isn't a failure of the install itself — don't let a restart
            // mid-download throttle the next genuine attempt.
            if (!succeeded && !interrupted)
                RecordFailedAttempt();
        }
    }

    private async Task<string?> DownloadLatestMsiAsync(CancellationToken cancellationToken)
    {
        var http = _httpClientFactory.CreateClient(nameof(ClamAvBootstrapService));

        GitHubRelease? release;
        try
        {
            // Read the response explicitly rather than via GetFromJsonAsync: on a non-success status
            // that helper throws with only the status code, discarding the body. GitHub puts the
            // actual reason there ("rate limit exceeded", "User-Agent required", ...), which is the
            // difference between a diagnosable log line and a bare 403.
            using var response = await http.GetAsync(LatestReleaseApiUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (body.Length > 500)
                    body = body[..500] + "…";

                _logger.LogError(
                    "GitHub returned {StatusCode} ({Reason}) when checking the latest ClamAV release; auto-install skipped for this app start. " +
                    "Rate limiting (60 requests/hour per IP for unauthenticated calls) and a rejected User-Agent both surface as 403 here. Response body: {Body}",
                    (int)response.StatusCode, response.ReasonPhrase, body);
                return null;
            }

            release = await response.Content.ReadFromJsonAsync<GitHubRelease>(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not reach GitHub to check the latest ClamAV release. Auto-install skipped for this app start.");
            return null;
        }

        // x64 vs win32 asset naming per the official ClamAV release (github.com/Cisco-Talos/clamav):
        // "clamav-<version>.win.x64.msi" / "clamav-<version>.win.win32.msi".
        var assetSuffix = Environment.Is64BitOperatingSystem ? "win.x64.msi" : "win.win32.msi";
        var asset = release?.Assets?.FirstOrDefault(a => a.Name.EndsWith(assetSuffix, StringComparison.OrdinalIgnoreCase));
        if (asset is null)
        {
            _logger.LogError("Could not find a Windows MSI asset (*.{Suffix}) in the latest ClamAV GitHub release. Auto-install skipped.", assetSuffix);
            return null;
        }

        var tempPath = Path.Combine(Path.GetTempPath(), asset.Name);
        _logger.LogInformation("Downloading {AssetName} ({SizeMb:N0} MB)...", asset.Name, asset.Size / 1024.0 / 1024.0);

        await using (var response = await http.GetStreamAsync(asset.BrowserDownloadUrl, cancellationToken))
        await using (var file = File.Create(tempPath))
        {
            await response.CopyToAsync(file, cancellationToken);
        }

        _logger.LogInformation("Download complete: {Path}.", tempPath);
        return tempPath;
    }

    private async Task<bool> RunMsiInstallAsync(string msiPath, CancellationToken cancellationToken)
    {
        var logPath = Path.Combine(Path.GetTempPath(), $"clamav-install-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.log");

        var startInfo = new ProcessStartInfo
        {
            FileName = "msiexec",
            Arguments = $"/i \"{msiPath}\" /quiet /norestart /l*v \"{logPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        Process? process;
        try
        {
            process = Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start msiexec. The app pool identity likely lacks permission to run installers on this host.");
            return false;
        }

        if (process is null)
        {
            _logger.LogError("msiexec did not start.");
            return false;
        }

        await process.WaitForExitAsync(cancellationToken);

        // 0 = success. 3010 = success, reboot recommended — fine for us, the installed files are
        // already usable without one. Anything else, especially in the 1600-1699 range, almost
        // always means "this identity isn't allowed to install software here".
        if (process.ExitCode is 0 or 3010)
        {
            _logger.LogInformation("ClamAV MSI installed (msiexec exit code {Code}).", process.ExitCode);
            return true;
        }

        _logger.LogError(
            "ClamAV MSI install failed — msiexec exited with code {Code}. See {LogPath} for msiexec's own verbose log.",
            process.ExitCode, logPath);
        return false;
    }

    /// <summary>
    /// The official Windows package ships without a <c>freshclam.conf</c> (see class remarks), so
    /// one is generated here with just the two directives freshclam actually needs, then freshclam
    /// is run once against it to populate <paramref name="clamScanPath"/>'s sibling "database" folder
    /// — the same folder <see cref="ClamAvVirusScanner"/> points clamscan at.
    /// </summary>
    private async Task SetUpVirusDatabaseAsync(string clamScanPath, CancellationToken cancellationToken)
    {
        var installDirectory = Path.GetDirectoryName(clamScanPath)!;
        var freshclamPath = Path.Combine(installDirectory, "freshclam.exe");
        if (!File.Exists(freshclamPath))
        {
            _logger.LogWarning("freshclam.exe not found at {Path} after install; virus database was not fetched. Scans will report as unavailable until this is resolved.", freshclamPath);
            return;
        }

        var databaseDirectory = Path.Combine(installDirectory, "database");
        Directory.CreateDirectory(databaseDirectory);

        var confPath = Path.Combine(installDirectory, "freshclam.generated.conf");
        await File.WriteAllTextAsync(confPath,
            $"""
             DatabaseDirectory {databaseDirectory}
             DatabaseMirror {DefaultDatabaseMirror}
             """, cancellationToken);

        _logger.LogInformation("Fetching initial ClamAV virus database (this can take a few minutes)...");

        var startInfo = new ProcessStartInfo
        {
            FileName = freshclamPath,
            Arguments = $"--config-file=\"{confPath}\"",
            WorkingDirectory = installDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            _logger.LogError("Failed to start freshclam.exe.");
            return;
        }

        var stdOut = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stdErr = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        // freshclam exit 0 = success (including "already up to date"). Anything else is logged but
        // not fatal to startup — clamscan will simply keep reporting ScanUnavailable (empty/missing
        // database) until this is retried, exactly like any other "scanner not ready yet" case.
        if (process.ExitCode == 0)
            _logger.LogInformation("ClamAV virus database ready at {Path}.", databaseDirectory);
        else
            _logger.LogError("freshclam exited with code {Code}. Output: {Output} {Error}", process.ExitCode, stdOut, stdErr);
    }

    private sealed class GitHubRelease
    {
        [JsonPropertyName("assets")]
        public List<GitHubReleaseAsset>? Assets { get; set; }
    }

    private sealed class GitHubReleaseAsset
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; } = "";

        [JsonPropertyName("size")]
        public long Size { get; set; }
    }
}
