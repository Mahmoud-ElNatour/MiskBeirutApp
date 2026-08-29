using System.ComponentModel.DataAnnotations;
using MiskBeirut.Core.Constants;

namespace MiskBeirut.Web.Areas.Admin.Models.Customers;

public class CustomerFormViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(FieldLengths.Name)]
    public string Name { get; set; } = "";

    [Required]
    [Phone]
    [StringLength(FieldLengths.PhoneNumber)]
    public string PhoneNumber { get; set; } = "";
}
