namespace MiskBeirut.Application.Dtos.Reviews;

public sealed record GoogleReviewDto
{
    public int Id { get; init; }
    public string AuthorName { get; init; } = null!;
    public string? ProfilePhotoUrl { get; init; }
    public int Rating { get; init; }
    public string? ReviewText { get; init; }
    public string? RelativeTime { get; init; }
}
