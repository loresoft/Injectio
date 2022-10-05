using System;
using System.Text;
using System.Text.RegularExpressions;

using Microsoft.CodeAnalysis;

namespace Injectio.Extensions;

public static class StringExtensions
{
    /// <summary>
    /// Truncates the specified text.
    /// </summary>
    /// <param name="text">The text to truncate.</param>
    /// <param name="keep">The number of characters to keep.</param>
    /// <param name="ellipsis">The ellipsis string to use when truncating. (Default ...)</param>
    /// <returns>
    /// A truncate string.
    /// </returns>
    public static string Truncate(this string text, int keep, string ellipsis = "...")
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        if (string.IsNullOrEmpty(ellipsis))
            ellipsis = string.Empty;

        if (text.Length <= keep)
            return text;

        if (text.Length <= keep + ellipsis.Length || keep < ellipsis.Length)
            return text.Substring(0, keep);

        return string.Concat(text.Substring(0, keep - ellipsis.Length), ellipsis);
    }

    /// <summary>
    /// Indicates whether the specified String object is null or an empty string
    /// </summary>
    /// <param name="item">A String reference</param>
    /// <returns>
    ///     <c>true</c> if is null or empty; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsNullOrEmpty(this string item)
    {
        return string.IsNullOrEmpty(item);
    }

    /// <summary>
    /// Indicates whether a specified string is null, empty, or consists only of white-space characters
    /// </summary>
    /// <param name="item">A String reference</param>
    /// <returns>
    ///      <c>true</c> if is null or empty; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsNullOrWhiteSpace(this string item)
    {
        if (item == null)
            return true;

        for (int i = 0; i < item.Length; i++)
            if (!char.IsWhiteSpace(item[i]))
                return false;

        return true;
    }

    /// <summary>
    /// Determines whether the specified string is not <see cref="IsNullOrEmpty"/>.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns>
    ///   <c>true</c> if the specified <paramref name="value"/> is not <see cref="IsNullOrEmpty"/>; otherwise, <c>false</c>.
    /// </returns>
    public static bool HasValue(this string value)
    {
        return !string.IsNullOrEmpty(value);
    }


    /// <summary>
    /// Appends a copy of the specified string if <paramref name="condition"/> is met.
    /// </summary>
    /// <param name="sb">The StringBuilder instance to append to.</param>
    /// <param name="text">The string to append.</param>
    /// <param name="condition">The condition delegate to evaluate. If condition is null, String.IsNullOrWhiteSpace method will be used.</param>
    public static IndentedStringBuilder AppendIf(this IndentedStringBuilder sb, string text, Func<string, bool> condition = null)
    {
        var c = condition ?? (s => !string.IsNullOrWhiteSpace(s));

        if (c(text))
            sb.Append(text);

        return sb;
    }

    /// <summary>
    /// Appends a copy of the specified string if <paramref name="condition"/> is met.
    /// </summary>
    /// <param name="sb">The StringBuilder instance to append to.</param>
    /// <param name="text">The string to append.</param>
    /// <param name="condition">The condition.</param>
    public static IndentedStringBuilder AppendIf(this IndentedStringBuilder sb, string text, bool condition)
    {
        if (condition)
            sb.Append(text);

        return sb;
    }

    /// <summary>
    /// Appends a copy of the specified string followed by the default line terminator if <paramref name="condition"/> is met.
    /// </summary>
    /// <param name="sb">The StringBuilder instance to append to.</param>
    /// <param name="text">The string to append.</param>
    /// <param name="condition">The condition delegate to evaluate. If condition is null, String.IsNullOrWhiteSpace method will be used.</param>
    public static IndentedStringBuilder AppendLineIf(this IndentedStringBuilder sb, string text, Func<string, bool> condition = null)
    {
        var c = condition ?? (s => !string.IsNullOrWhiteSpace(s));

        if (c(text))
            sb.AppendLine(text);

        return sb;
    }


    /// <summary>
    /// Converts the specified <paramref name="text"/> into spaced words.
    /// </summary>
    /// <param name="text">The text to convert.</param>
    /// <returns>The text with a space before every capital letter</returns>
    public static string ToTitle(this string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length < 2)
            return text;

        var words = Regex.Matches(text, "([A-Z][a-z]*)|([0-9]+)") as IEnumerable<Match>;
        return string.Join(" ", words.Select(w => w.Value));
    }
}
