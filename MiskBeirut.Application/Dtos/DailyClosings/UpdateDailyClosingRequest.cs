namespace MiskBeirut.Application.Dtos.DailyClosings;

public sealed record UpdateDailyClosingRequest
{
    public decimal? AdjustedReading { get; init; }
    public decimal? ActualCash { get; init; }
    public string? Note { get; init; }
}
