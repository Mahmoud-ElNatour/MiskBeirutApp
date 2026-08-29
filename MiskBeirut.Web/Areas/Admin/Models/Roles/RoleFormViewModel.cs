using System.ComponentModel.DataAnnotations;

namespace MiskBeirut.Web.Areas.Admin.Models.Roles;

public class RoleFormViewModel
{
    [Required]
    public string Name { get; set; } = "";
}
