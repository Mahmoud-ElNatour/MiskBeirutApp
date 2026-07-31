namespace MiskBeirut.Application.Dtos.DailyClosings;

public sealed record DailyClosingDto
{
    public int Id { get; init; }
    public DateOnly Date { get; init; }
    public decimal MainReading { get; init; }
    public decimal? AdjustedReading { get; init; }
    public decimal? ActualCash { get; init; }
    public string? Note { get; init; }
    public DateTime CreatedAt { get; init; }
}
