using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MiskBeirut.Core.Constants;

namespace MiskBeirut.Web.Areas.Admin.Models.Customers;

// Field names match the ported legacy JS payload (Areas/Admin/Views/Customers/_AddModal.cshtml / _EditModal.cshtml).
public class CustomerApiRequest
{
    [JsonPropertyName("username")]
    [Required]
    [StringLength(FieldLengths.Name)]
    public string Username { get; set; } = "";

    [JsonPropertyName("phone_number")]
    [Phone]
    [StringLength(FieldLengths.PhoneNumber)]
    public string? PhoneNumber { get; set; }

    [JsonPropertyName("balance")]
    public decimal Balance { get; set; }
}
