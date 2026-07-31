using System.ComponentModel.DataAnnotations;

namespace MiskBeirut.Web.Areas.Admin.Models.Customers;

public class CustomerFormViewModel
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = "";

    [Required]
    public string PhoneNumber { get; set; } = "";
}
