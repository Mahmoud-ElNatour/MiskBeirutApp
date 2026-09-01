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
    [RegularExpression(ValidationPatterns.PhoneNumber)]
    [StringLength(FieldLengths.PhoneNumber)]
    public string PhoneNumber { get; set; } = "";

    [Required]
    [RegularExpression(ValidationPatterns.Email)]
    [StringLength(FieldLengths.Email)]
    public string Email { get; set; } = "";

    [StringLength(FieldLengths.Address)]
    public string? Address { get; set; }

    [Range(1, int.MaxValue)]
    public int VacancyId { get; set; }

    [Required]
    public IFormFile Cv { get; set; } = null!;
}
