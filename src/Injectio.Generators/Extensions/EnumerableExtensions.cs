using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Injectio.Generators.Extensions;

[ExcludeFromCodeCoverage]
public static class EnumerableExtensions
{
    /// <summary>
    /// Concatenates the string representations of the elements in a sequence, using the specified delimiter between each element.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the sequence.</typeparam>
    /// <param name="values">The sequence of values to concatenate. Each value will be converted to a string using its <see cref="object.ToString()"/> method.</param>
    /// <param name="delimiter">The string to use as a delimiter between elements. If <see langword="null"/>, a comma (",") is used by default.</param>
    /// <returns>
    /// A string that consists of the string representations of the elements in <paramref name="values"/> delimited by the <paramref name="delimiter"/> string.
    /// If <paramref name="values"/> is empty, returns <see cref="string.Empty"/>.
    /// </returns>
    /// <remarks>
    /// <see langword="null"/> elements in the sequence will be converted to empty strings in the result.
    /// This method is equivalent to calling <see cref="string.Join{T}(string, IEnumerable{T})"/> with the provided parameters.
    /// </remarks>
    public static string ToDelimitedString<T>(this IEnumerable<T?> values, string? delimiter = ",")
        => string.Join(delimiter ?? ",", values);

    /// <summary>
    /// Concatenates the elements of a string sequence, using the specified delimiter between each element.
    /// </summary>
    /// <param name="values">The sequence of string values to concatenate.</param>
    /// <param name="delimiter">The string to use as a delimiter between elements. If <see langword="null"/>, a comma (",") is used by default.</param>
    /// <returns>
    /// A string that consists of the elements in <paramref name="values"/> delimited by the <paramref name="delimiter"/> string.
    /// If <paramref name="values"/> is empty, returns <see cref="string.Empty"/>.
    /// </returns>
    /// <remarks>
    /// <see langword="null"/> elements in the sequence will be treated as empty strings in the result.
    /// This method is equivalent to calling <see cref="string.Join(string, IEnumerable{string})"/> with the provided parameters.
    /// </remarks>
    public static string ToDelimitedString(this IEnumerable<string?> values, string? delimiter = ",")
        => string.Join(delimiter ?? ",", values);
}
