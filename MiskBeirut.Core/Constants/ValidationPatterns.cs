namespace MiskBeirut.Core.Constants;

/// <summary>
/// Regular expressions shared by the public forms' server-side <c>[RegularExpression]</c> attributes
/// and the same forms' HTML <c>pattern</c> attributes, so a value the browser accepts is a value the
/// server accepts and vice versa. Deliberately stricter than <c>[Phone]</c>/<c>[EmailAddress]</c>,
/// which accept things a visitor plainly mistyped: <c>[EmailAddress]</c> passes "ddd@s" (no dot in
/// the domain at all), and neither gives the visitor a field-level reason for the rejection.
/// </summary>
public static class ValidationPatterns
{
    /// <summary>
    /// International or local phone number: an optional leading "+", then digits with spaces,
    /// dashes, dots or parentheses as grouping. The lookahead requires at least 6 actual digits, so
    /// punctuation alone ("---- --") can't satisfy the length.
    /// </summary>
    public const string PhoneNumber = @"^(?=(?:\D*\d){6,})\+?[0-9\s().\-]{6,25}$";

    /// <summary>
    /// Email address with a dotted domain — "name@example.com", not "name@example". No spaces or
    /// second "@" anywhere.
    /// </summary>
    public const string Email = @"^[^@\s]+@[^@\s.]+(\.[^@\s.]+)+$";
}
