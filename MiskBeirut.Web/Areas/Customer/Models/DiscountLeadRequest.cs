using System.ComponentModel.DataAnnotations;
using MiskBeirut.Core.Constants;

namespace MiskBeirut.Web.Areas.Customer.Models;

public class DiscountLeadRequest
{
    [Required]
    [StringLength(FieldLengths.Name)]
    public string Name { get; set; } = "";

    [Required]
    [EmailAddress]
    [StringLength(FieldLengths.Email)]
    public string Email { get; set; } = "";

    [Required]
    [Phone]
    [StringLength(FieldLengths.PhoneNumber)]
    public string PhoneNumber { get; set; } = "";
}
