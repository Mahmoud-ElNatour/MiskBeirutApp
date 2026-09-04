using System.Text.Json;
using MiskBeirut.Application.Dtos.Careers;

namespace MiskBeirut.Web.Support;

/// <summary>
/// Builds the JSON-LD block the public layout emits — the machine-readable half of every page.
///
/// Written as one @graph rather than several loose scripts so the nodes can reference each other by
/// @id: the page says it is part of the website, the website says the restaurant publishes it, and
/// each job posting names the same restaurant as its hiring organization. Repeating the business as
/// an unlinked island in each block is what makes Google treat one restaurant as several.
///
/// Serialized rather than written as text in the view: every value here is editor-supplied, and a
/// quote mark or an angle bracket typed into the Cms would otherwise end the script tag early.
/// </summary>
public static class StructuredData
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = false };

    public static string Build(
        PageContent content,
        string lang,
        string baseUrl,
        string? canonicalUrl,
        string? pageTitle,
        string? pageDescription,
        string? shareImage,
        string menuUrl,
        IReadOnlyList<VacancyDto>? vacancies)
    {
        var business = new BusinessProfile(content);
        var businessId = baseUrl + "/#business";
        var websiteId = baseUrl + "/#website";

        var graph = new List<object>
        {
            Node(
                ("@type", "WebSite"),
                ("@id", websiteId),
                ("url", baseUrl),
                ("name", business.Name),
                ("inLanguage", lang),
                ("publisher", Reference(businessId))),

            // Restaurant is a subtype of both LocalBusiness and Organization, so one node carries the
            // logo, address, phone, hours and social profiles rather than three nodes disagreeing.
            Node(
                ("@type", "Restaurant"),
                ("@id", businessId),
                ("name", business.Name),
                ("url", baseUrl),
                ("logo", Absolute(baseUrl, business.Logo)),
                ("image", shareImage),
                ("telephone", business.PhoneLink),
                ("email", business.Email),
                ("priceRange", business.PriceRange),
                ("servesCuisine", business.ServesCuisine),
                ("hasMenu", menuUrl),
                ("address", Node(
                    ("@type", "PostalAddress"),
                    ("streetAddress", business.AddressStreet),
                    ("addressLocality", business.AddressLocality),
                    ("postalCode", business.PostalCode),
                    ("addressCountry", business.AddressCountry))),
                ("geo", business.Latitude is null || business.Longitude is null
                    ? null
                    : Node(
                        ("@type", "GeoCoordinates"),
                        ("latitude", business.Latitude),
                        ("longitude", business.Longitude))),
                ("openingHours", business.OpeningHours.Count > 0 ? business.OpeningHours : null),
                ("sameAs", business.SocialProfiles.Count > 0 ? business.SocialProfiles : null)),

            Node(
                ("@type", "WebPage"),
                ("@id", canonicalUrl is null ? null : canonicalUrl + "#webpage"),
                ("url", canonicalUrl),
                ("name", pageTitle),
                ("description", pageDescription),
                ("inLanguage", lang),
                ("isPartOf", Reference(websiteId)),
                ("about", Reference(businessId)))
        };

        if (vacancies is { Count: > 0 })
            graph.AddRange(vacancies.Select(v => JobPosting(v, business, businessId, canonicalUrl, lang)));

        return JsonSerializer.Serialize(
            new Dictionary<string, object?> { ["@context"] = "https://schema.org", ["@graph"] = graph },
            SerializerOptions);
    }

    /// <summary>
    /// One open position. Google for Jobs rejects a posting without title, description, datePosted
    /// and a hiring organization, so a vacancy with no description falls back to its own title
    /// rather than emitting an entry that will only ever be reported as an error.
    /// </summary>
    private static object JobPosting(VacancyDto vacancy, BusinessProfile business, string businessId, string? canonicalUrl, string lang)
    {
        var description = string.IsNullOrWhiteSpace(vacancy.Description)
            ? vacancy.Title
            : vacancy.Description;

        if (vacancy.RequirementLines.Count > 0)
            description += "\n\n" + string.Join("\n", vacancy.RequirementLines.Select(line => "- " + line));

        return Node(
            ("@type", "JobPosting"),
            ("title", vacancy.Title),
            ("description", description),
            ("identifier", Node(
                ("@type", "PropertyValue"),
                ("name", business.Name),
                ("value", vacancy.Slug))),
            ("datePosted", vacancy.CreatedAt.ToString("yyyy-MM-dd")),
            ("validThrough", vacancy.ApplicationDeadline?.ToString("yyyy-MM-dd")),
            ("employmentType", EmploymentType(vacancy.EmploymentType)),
            ("inLanguage", lang),
            ("directApply", true),
            ("url", canonicalUrl is null ? null : $"{canonicalUrl}#{vacancy.Slug}"),
            ("hiringOrganization", Reference(businessId)),
            ("jobLocation", Node(
                ("@type", "Place"),
                ("address", Node(
                    ("@type", "PostalAddress"),
                    ("streetAddress", business.AddressStreet),
                    ("addressLocality", business.AddressLocality),
                    ("addressCountry", business.AddressCountry))))));
    }

    /// <summary>
    /// schema.org expects an enum ("FULL_TIME"), not the label an editor typed ("Full time", or its
    /// Arabic equivalent). An unrecognized value is left out rather than guessed at.
    /// </summary>
    private static string? EmploymentType(string employmentType)
    {
        var normalized = new string(employmentType.Where(char.IsLetter).ToArray()).ToLowerInvariant();
        return normalized switch
        {
            "fulltime" => "FULL_TIME",
            "parttime" => "PART_TIME",
            "contract" or "contractor" => "CONTRACTOR",
            "temporary" or "seasonal" => "TEMPORARY",
            "internship" or "intern" => "INTERN",
            _ => null
        };
    }

    /// <summary>
    /// A schema.org node with its empty fields dropped. Emitting "telephone": null or an empty
    /// address is a validation error in Search Console, and an editor who hasn't filled a field in
    /// yet should produce a smaller graph, not a broken one.
    /// </summary>
    private static Dictionary<string, object?> Node(params (string Key, object? Value)[] fields)
    {
        var node = new Dictionary<string, object?>();
        foreach (var (key, value) in fields)
        {
            if (value is null || (value is string text && string.IsNullOrWhiteSpace(text)))
                continue;

            node[key] = value;
        }

        return node;
    }

    private static Dictionary<string, object?> Reference(string id) => new() { ["@id"] = id };

    private static string? Absolute(string baseUrl, string? path)
        => path is null || path.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? path
            : baseUrl + (path.StartsWith('/') ? path : "/" + path);
}
