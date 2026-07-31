namespace MiskBeirut.Web.Areas.Admin.Models.DailyClosing;

public class EditDailyClosingViewModel
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public decimal MainReading { get; set; }
    public decimal? AdjustedReading { get; set; }
    public decimal? ActualCash { get; set; }
    public string? Note { get; set; }
}
