namespace MiskBeirut.Web.Areas.Admin.Models.Receivers;

// Field name matches the ported legacy JS payload (Areas/Admin/Views/Receivers/_AddModal.cshtml / _EditModal.cshtml).
public class ReceiverApiRequest
{
    public string Name { get; set; } = "";
}
