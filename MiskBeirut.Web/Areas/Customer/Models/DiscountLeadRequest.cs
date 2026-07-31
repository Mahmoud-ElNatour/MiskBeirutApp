using System.ComponentModel.DataAnnotations;

namespace MiskBeirut.Web.Areas.Customer.Models;

public class DiscountLeadRequest
{
    [Required]
    public string Name { get; set; } = "";

    [Required]
    [EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    [Phone]
    public string PhoneNumber { get; set; } = "";
}
