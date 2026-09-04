using Microsoft.AspNetCore.Routing.Constraints;

namespace MiskBeirut.Web.Support;

/// <summary>
/// Backs the <c>{lang:lang}</c> segment of the public route, so only the languages the site actually
/// publishes can open a page. Without it, "/fr/about" and "/anything/about" would both match and
/// render the English page at an unbounded number of addresses — every one of them a duplicate a
/// crawler could find, and none of them canonical.
///
/// Registered under the name "lang" in Program.cs's ConstraintMap; the list it checks against is
/// <see cref="SiteLanguages.All"/>, so adding a language stays a one-line change there.
/// </summary>
public sealed class LanguageRouteConstraint : IRouteConstraint
{
    public bool Match(HttpContext? httpContext, IRouter? route, string routeKey,
        RouteValueDictionary values, RouteDirection routeDirection)
        => values.TryGetValue(routeKey, out var value)
           && SiteLanguages.IsSupported(Convert.ToString(value));
}
