using Microsoft.Extensions.Logging;
using MiskBeirut.Application.Services;

namespace MiskBeirut.Infrastructure.Services;

/// <summary>
/// Tries each scanner in order and returns the first definitive verdict (Clean or Infected).
/// Only reports <see cref="VirusScanOutcome.ScanUnavailable"/> if every scanner in the chain was
/// unavailable — e.g. Windows Defender first, ClamAV as a fallback if this host doesn't have
/// Defender installed. A file is only ever scanned by ONE engine (the first that's actually
/// available), not all of them.
/// </summary>
public class FallbackVirusScanner : IVirusScanner
{
    private readonly IReadOnlyList<IVirusScanner> _scanners;
    private readonly ILogger<FallbackVirusScanner> _logger;

    public FallbackVirusScanner(IReadOnlyList<IVirusScanner> scanners, ILogger<FallbackVirusScanner> logger)
    {
        if (scanners.Count == 0)
            throw new ArgumentException("At least one scanner is required.", nameof(scanners));

        _scanners = scanners;
        _logger = logger;
    }

    public async Task<VirusScanOutcome> ScanAsync(string filePath, CancellationToken cancellationToken = default)
    {
        for (var i = 0; i < _scanners.Count; i++)
        {
            var outcome = await _scanners[i].ScanAsync(filePath, cancellationToken);
            if (outcome != VirusScanOutcome.ScanUnavailable)
                return outcome;

            if (i < _scanners.Count - 1)
                _logger.LogWarning("{Scanner} was unavailable for {File}; trying the next configured scanner.", _scanners[i].GetType().Name, filePath);
        }

        _logger.LogError("All {Count} configured virus scanner(s) were unavailable for {File}.", _scanners.Count, filePath);
        return VirusScanOutcome.ScanUnavailable;
    }
}
