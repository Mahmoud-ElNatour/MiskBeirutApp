namespace MiskBeirut.Web.Support;

/// <summary>
/// The English/Arabic pair for a status or validation message returned by a public form endpoint.
/// The public site has no resource files — page copy is CMS-managed per language (see
/// <see cref="PageContent"/>) — but a validation failure has no CMS attribute to read from, so the
/// two variants are carried here and picked by the visitor's current language. Without this, an
/// Arabic visitor gets an English error back from an otherwise fully Arabic page.
/// </summary>
public readonly struct PublicMessages
{
    private readonly bool _isArabic;

    public PublicMessages(string? langCode)
    {
        _isArabic = string.Equals(langCode, "ar", StringComparison.OrdinalIgnoreCase);
    }

    public string Pick(string english, string arabic) => _isArabic ? arabic : english;
}
