using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using MiskBeirut.Core.Constants;

namespace MiskBeirut.Web.Areas.Customer.Models;

public class JobApplicationRequest
{
    [Required]
    [StringLength(FieldLengths.Name)]
    public string Name { get; set; } = "";

    [Required]
    [Phone]
    [StringLength(FieldLengths.PhoneNumber)]
    public string PhoneNumber { get; set; } = "";

    [Required]
    [EmailAddress]
    [StringLength(FieldLengths.Email)]
    public string Email { get; set; } = "";

    [StringLength(FieldLengths.Address)]
    public string? Address { get; set; }

    [Required]
    public int VacancyId { get; set; }

    [Required]
    public IFormFile Cv { get; set; } = null!;
}
