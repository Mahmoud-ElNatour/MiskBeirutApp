namespace MiskBeirut.Web.Support;

/// <summary>
/// The restaurant's name, address, phone, email and hours, read once from the Global CMS page.
///
/// These used to be typed separately into each place that showed them — the footer had one phone
/// number as its default, the Contact page a different one, the tel: link a third, and the WhatsApp
/// button a fourth. To a search engine that inconsistency is the whole of local SEO: the business
/// looks like several near-identical businesses, and none of them matches the Google Business
/// Profile. There is now one set of Global attributes, every surface reads from it, and structured
/// data publishes the same values it displays.
/// </summary>
public sealed class BusinessProfile
{
    public const string PhoneAttribute = "contact_phone";

    private readonly PageContent _content;

    public BusinessProfile(PageContent content) => _content = content;

    public string Name => _content.Global("brand_name", "Misk Beirut");

    /// <summary>The number as a human reads it, spaced for the language it's shown in.</summary>
    public string Phone => _content.Global(PhoneAttribute, "+961 76 551 204");

    /// <summary>The same number as a tel: href — digits and a leading +, nothing else.</summary>
    public string PhoneLink => Digits(Phone);

    /// <summary>wa.me wants the number with no + and no separators.</summary>
    public string WhatsAppUrl => _content.Global("contact_whatsapp_url", $"https://wa.me/{Digits(Phone).TrimStart('+')}");

    public string Email => _content.Global("contact_email", "hello@miskbeirut.com");

    /// <summary>The address on one line, as the Contact card and the footer print it.</summary>
    public string AddressLine => _content.Global("address_line", "Ramleh el Bayda, Farid Trad Street, Beirut", "الرملة البيضاء، شارع فريد طراد، بيروت");

    public string AddressStreet => _content.Global("address_street", "Farid Trad Street, Ramleh el Bayda");
    public string AddressLocality => _content.Global("address_locality", "Beirut", "بيروت");
    public string AddressCountry => _content.Global("address_country", "LB");
    public string? PostalCode => Blank(_content.Global("address_postal_code"));

    /// <summary>
    /// Opening hours in schema.org's own notation ("Mo-Su 08:00-01:00"), one span per line. Blank
    /// until someone fills it in: publishing invented hours for a real restaurant is worse than
    /// publishing none, because Google will show them.
    /// </summary>
    public IReadOnlyList<string> OpeningHours => _content.Global("opening_hours")
        .Split(['\r', '\n', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public string? PriceRange => Blank(_content.Global("price_range"));
    public string ServesCuisine => _content.Global("serves_cuisine", "Lebanese");

    public string? Latitude => Blank(_content.Global("geo_latitude"));
    public string? Longitude => Blank(_content.Global("geo_longitude"));

    public string? Logo => Blank(_content.Global("logo_image"));

    /// <summary>Instagram, Facebook and anything else the CMS knows about — schema.org's sameAs.</summary>
    public IReadOnlyList<string> SocialProfiles =>
        new[] { _content.Global("social_instagram_url"), _content.Global("social_facebook_url") }
            .Where(url => !string.IsNullOrWhiteSpace(url) && url != "#")
            .ToList();

    private static string? Blank(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private static string Digits(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        return digits.Length == 0 ? "" : "+" + digits;
    }
}
