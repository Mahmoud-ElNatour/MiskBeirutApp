namespace MiskBeirut.Core.Entities;

/// <summary>A Google Maps review, populated externally (scraped) and shown on the homepage.</summary>
public class GoogleReview
{
    public int Id { get; set; }
    public string AuthorName { get; set; } = null!;
    public string? ProfilePhotoUrl { get; set; }
    public int Rating { get; set; }
    public string? ReviewText { get; set; }
    public string? RelativeTime { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
}
