namespace MiskBeirut.Core.Enums;

/// <summary>Stored as a string in customer.page_attributes.AttributeType.</summary>
public enum PageAttributeType
{
    Text,
    RichText,
    Image,
    Link,
    Video,
    Number,
    Date,
    Boolean,

    /// <summary>
    /// A PDF the Cms uploads and the site links to or embeds — currently the menu. Stores the same
    /// kind of value a Link does (a /pdf/cms/... url); the separate type is what tells the editor to
    /// offer a file picker instead of a text box.
    /// </summary>
    Pdf
}
