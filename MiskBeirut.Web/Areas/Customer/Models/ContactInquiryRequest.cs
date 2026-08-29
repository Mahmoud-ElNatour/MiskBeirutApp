using System.ComponentModel.DataAnnotations;
using MiskBeirut.Core.Constants;

namespace MiskBeirut.Web.Areas.Customer.Models;

public class ContactInquiryRequest
{
    [Required]
    [StringLength(FieldLengths.Name)]
    public string FullName { get; set; } = "";

    [Required]
    [Phone]
    [StringLength(FieldLengths.PhoneNumber)]
    public string PhoneNumber { get; set; } = "";

    [EmailAddress]
    [StringLength(FieldLengths.Email)]
    public string? Email { get; set; }

    [Required]
    [StringLength(FieldLengths.Message)]
    public string Message { get; set; } = "";

    [Required]
    public int ReasonId { get; set; }
}
