using System.ComponentModel.DataAnnotations;

namespace MiskBeirut.Web.Areas.Admin.Models.DailyClosing;

public class CreateDailyClosingViewModel
{
    [Required]
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required]
    public decimal MainReading { get; set; }

    public decimal? AdjustedReading { get; set; }

    public decimal? ActualCash { get; set; }

    public string? Note { get; set; }
}
