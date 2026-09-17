using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Phymnary.SugarPot.AspNetCore.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Converts a PascalCase string into kebab-case.
    /// </summary>
    /// <param name="value">The input string in PascalCase format.</param>
    /// <returns>The kebab-case representation of <paramref name="value" />.</returns>
    public static string PascalToKebabCase(this string value)
    {
        return PascalToKebabMyRegex().Replace(value, "-$1").Trim().ToLower();
    }

    /// <summary>
    /// Splits a string by semicolon and removes blank entries.
    /// </summary>
    /// <param name="value">The source string.</param>
    /// <returns>
    /// An array of non-empty, trimmed segments separated by semicolons.
    /// </returns>
    public static string[] SplitBySemicolon(this string value)
    {
        return value.Split(
            ";",
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );
    }

    /// <summary>
    /// Determines whether a string is <see langword="null" />, empty, or whitespace.
    /// </summary>
    /// <param name="value">The string to evaluate.</param>
    /// <returns><see langword="true" /> if the string is blank; otherwise, <see langword="false" />.</returns>
    public static bool IsBlank([NotNullWhen(false)] this string? value)
    {
        return string.IsNullOrWhiteSpace(value);
    }

    /// <summary>
    /// Attempts to return a non-blank value through an output variable.
    /// </summary>
    /// <param name="value">The source string value.</param>
    /// <param name="variable">The output variable that receives <paramref name="value" />.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="value" /> is not blank; otherwise, <see langword="false" />.
    /// </returns>
    public static bool TryGetValuable(this string? value, [NotNullWhen(true)] out string? variable)
    {
        variable = value;
        return !string.IsNullOrWhiteSpace(value);
    }

    /// <summary>
    /// Returns <see langword="null" /> when the input is blank; otherwise returns the original value.
    /// </summary>
    /// <param name="text">The source string value.</param>
    /// <returns><see langword="null" /> if blank; otherwise <paramref name="text" />.</returns>
    public static string? NullIfBlank(this string? text)
    {
        return text.IsBlank() ? null : text;
    }

    /// <summary>
    /// Removes a postfix string when the input ends with that postfix.
    /// </summary>
    /// <param name="str">The source string.</param>
    /// <param name="postFix">The postfix to remove.</param>
    /// <returns>The string without the postfix when matched; otherwise the original string.</returns>
    public static string StripPostfix(this string str, string postFix)
    {
        return str.EndsWith(postFix) ? str[..^postFix.Length] : str;
    }

    /// <summary>
    /// Removes a postfix character when the input ends with that character.
    /// </summary>
    /// <param name="str">The source string.</param>
    /// <param name="postFix">The postfix character to remove.</param>
    /// <returns>The string without the postfix character when matched; otherwise the original string.</returns>
    public static string StripPostfix(this string str, char postFix)
    {
        return str.EndsWith(postFix) ? str[..^1] : str;
    }

    /// <summary>
    /// Removes a prefix string when the input starts with that prefix.
    /// </summary>
    /// <param name="str">The source string.</param>
    /// <param name="value">The prefix to remove.</param>
    /// <returns>The string without the prefix when matched; otherwise the original string.</returns>
    public static string StripPrefix(this string str, string value)
    {
        if (str.StartsWith(value))
            str = str[value.Length..];

        return str;
    }

    /// <summary>
    /// Removes a prefix character when the input starts with that character.
    /// </summary>
    /// <param name="str">The source string.</param>
    /// <param name="value">The prefix character to remove.</param>
    /// <returns>The string without the prefix character when matched; otherwise the original string.</returns>
    public static string StripPrefix(this string str, char value)
    {
        if (str.StartsWith(value))
            str = str[1..];

        return str;
    }

    /// <summary>
    /// Determines whether a string represents a truthy value.
    /// </summary>
    /// <param name="text">The source string.</param>
    /// <returns>
    /// <see langword="true" /> when the normalized value is <c>TRUE</c>, <c>1</c>, or <c>T</c>;
    /// otherwise <see langword="false" />.
    /// </returns>
    public static bool IsTruthy(this string? text)
    {
        if (text.IsBlank())
            return false;

        text = text.ToUpperInvariant().Trim();

        return text is "TRUE" or "1" or "T";
    }

    [GeneratedRegex("(?<!^)([A-Z][a-z]|(?<=[a-z])[A-Z0-9])")]
    private static partial Regex PascalToKebabMyRegex();
}
