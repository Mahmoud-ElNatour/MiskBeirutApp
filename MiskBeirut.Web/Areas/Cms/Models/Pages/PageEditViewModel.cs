namespace MiskBeirut.Web.Areas.Cms.Models.Pages;

public class PageEditViewModel
{
    public int PageId { get; set; }
    public string PageName { get; set; } = "";

    public int LangId { get; set; }
    public string LangCode { get; set; } = "en";
    public List<LanguageOptionViewModel> Languages { get; set; } = [];

    public List<PageAttributeRowViewModel> Attributes { get; set; } = [];
}

public class LanguageOptionViewModel
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
}

public class PageAttributeRowViewModel
{
    public string AttributeName { get; set; } = "";
    public string AttributeType { get; set; } = "Text";
    public string? Value { get; set; }
}
