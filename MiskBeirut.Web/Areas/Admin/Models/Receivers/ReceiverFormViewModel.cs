using System.ComponentModel.DataAnnotations;

namespace MiskBeirut.Web.Areas.Admin.Models.Receivers;

public class ReceiverFormViewModel
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = "";
}
