# Registers a Windows Scheduled Task that runs the review scraper every 3 days.
# Usage (after publishing the project):
#   dotnet publish -c Release
#   .\register-scheduled-task.ps1
#
# To run unattended on a server (no user logged in), register instead as SYSTEM:
#   .\register-scheduled-task.ps1 -RunAsSystem

param(
    [string]$TaskName = "MiskBeirut-ReviewScraper",
    [string]$ExePath = (Join-Path $PSScriptRoot "bin\Release\net8.0\MiskBeirut.ReviewScraper.exe"),
    [datetime]$StartTime = (Get-Date -Hour 3 -Minute 0 -Second 0),
    [switch]$RunAsSystem
)

if (-not (Test-Path $ExePath)) {
    Write-Error "Executable not found at '$ExePath'. Publish the project first (dotnet publish -c Release), or pass -ExePath explicitly."
    exit 1
}

$action = New-ScheduledTaskAction -Execute $ExePath -WorkingDirectory (Split-Path $ExePath)
$trigger = New-ScheduledTaskTrigger -Once -At $StartTime -RepetitionInterval (New-TimeSpan -Days 3) -RepetitionDuration (New-TimeSpan -Days 3650)
$settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -DontStopOnIdleEnd -ExecutionTimeLimit (New-TimeSpan -Hours 1)

if ($RunAsSystem) {
    Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $trigger -Settings $settings `
        -User "SYSTEM" -RunLevel Highest `
        -Description "Scrapes the top 3 five-star Google Maps reviews into customer.google_reviews every 3 days." -Force
} else {
    Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $trigger -Settings $settings `
        -Description "Scrapes the top 3 five-star Google Maps reviews into customer.google_reviews every 3 days." -Force
}

Write-Host "Scheduled task '$TaskName' registered - first run at $StartTime, repeating every 3 days."
Write-Host "Test it immediately with: Start-ScheduledTask -TaskName '$TaskName'"
