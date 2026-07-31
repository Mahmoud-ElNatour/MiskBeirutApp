namespace MiskBeirut.Application.Dtos.DailyClosings;

public sealed record CreateDailyClosingRequest
{
    public DateOnly Date { get; init; }
    public decimal MainReading { get; init; }
    public decimal? AdjustedReading { get; init; }
    public decimal? ActualCash { get; init; }
    public string? Note { get; init; }
}
