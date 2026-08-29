using System.ComponentModel.DataAnnotations;

namespace MiskBeirut.Web.Areas.Admin.Models.Investors;

public class InvestorFormViewModel
{
    [Required]
    public string Name { get; set; } = "";
}
