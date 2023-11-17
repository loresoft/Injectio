namespace Injectio.Generators;


/// <summary>
/// An immutable hash code structure
/// </summary>
/// <remarks>
/// Implements the Jon Skeet suggested implementation of GetHashCode().
/// </remarks>
public readonly struct HashCode : IFormattable, IEquatable<HashCode>
{
    /// <summary>
    /// The prime multiplier used to combine hash codes.
    /// </summary>
    public const int Multiplier = 31;

    private readonly int _hashCode;

    /// <summary>
    /// Initializes a new instance of the <see cref="HashCode"/> struct.
    /// </summary>
    /// <param name="hashCode">The hash code.</param>
    public HashCode(int hashCode)
    {
        _hashCode = hashCode;
    }

    /// <summary>
    /// Gets a hash code seed value for combine hash codes values.
    /// </summary>
    /// <value>
    /// The hash code seed value.
    /// </value>
    public static HashCode Seed => new(17);

    /// <summary>
    /// Combines this hash code with the hash code of specified <paramref name="value" />.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="value">The value to combine hash codes with.</param>
    /// <returns>A new hash code combined with this and the values hash codes.</returns>
    public HashCode Combine<TValue>(TValue? value)
    {
        var hashCode = value is null ? 0 : EqualityComparer<TValue>.Default.GetHashCode(value);
        unchecked
        {
            hashCode = (_hashCode * Multiplier) + hashCode;
        }

        return new HashCode(hashCode);
    }

    /// <summary>
    /// Combines this hash code with the hash code of specified <paramref name="value" />.
    /// </summary>
    /// <param name="value">The value to combine hash codes with.</param>
    /// <returns>A new hash code combined with this and the values hash codes.</returns>
    public HashCode Combine(string? value)
    {
        // need to handle string values deterministically
        var hashCode = HashString(value);
        unchecked
        {
            hashCode = (_hashCode * Multiplier) + hashCode;
        }

        return new HashCode(hashCode);
    }

    /// <summary>
    /// Combines this hash code with the hash code of specified <paramref name="value" />.
    /// </summary>
    /// <param name="value">The value to combine hash codes with.</param>
    /// <returns>A new hash code combined with this and the values hash codes.</returns>
    public HashCode Combine(object? value)
    {
        // need to handle string values deterministically
        return value switch
        {
            string text => Combine(text),
            _ => Combine(value?.GetHashCode() ?? 0),
        };
    }

    /// <summary>
    /// Combines this hash code with the hash code of each item specified <paramref name="values" />.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="values">The values to combine hash codes with.</param>
    /// <returns>A new hash code combined with this and the values hash codes.</returns>
    public HashCode CombineAll<TValue>(IEnumerable<TValue>? values)
    {
        if (values == null)
            return this;

        var comparer = EqualityComparer<TValue>.Default;
        var current = _hashCode;

        foreach (var value in values)
        {
            var hashCode = value switch
            {
                string text => HashString(text),
                TValue instance => comparer.GetHashCode(instance),
                _ => 0
            };

            unchecked
            {
                hashCode = (current * Multiplier) + hashCode;
            }

            current = hashCode;
        }

        return new HashCode(current);
    }


    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    /// <returns>
    /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
    /// </returns>
    public override int GetHashCode() => _hashCode;


    /// <summary>
    /// Converts the numeric value of this instance to its equivalent string representation.
    /// </summary>
    /// <returns>
    /// The string representation of the value of this instance.
    /// </returns>
    public override string ToString() => _hashCode.ToString();

    /// <summary>
    /// Converts the numeric value of this instance to its equivalent string representation using the specified culture-specific format information.
    /// </summary>
    /// <param name="provider">An object that supplies culture-specific formatting information.</param>
    /// <returns>
    /// The string representation of the value of this instance as specified by provider.
    /// </returns>
    public string ToString(IFormatProvider provider) => _hashCode.ToString(provider);

    /// <summary>
    /// Converts the numeric value of this instance to its equivalent string representation using the specified format.
    /// </summary>
    /// <param name="format">A standard or custom numeric format string.</param>
    /// <returns>
    /// The string representation of the value of this instance as specified by format.
    /// </returns>
    public string ToString(string format) => _hashCode.ToString(format);

    /// <summary>
    /// Converts the numeric value of this instance to its equivalent string representation using the specified format and culture-specific format information.
    /// </summary>
    /// <param name="format">A standard or custom numeric format string.</param>
    /// <param name="provider">An object that supplies culture-specific formatting information.</param>
    /// <returns>
    /// The string representation of the value of this instance as specified by format and provider.
    /// </returns>
    public string ToString(string format, IFormatProvider provider) => _hashCode.ToString(format, provider);


    /// <summary>
    /// Determines whether the specified <see cref="object" />, is equal to this instance.
    /// </summary>
    /// <param name="other">The <see cref="object" /> to compare with this instance.</param>
    /// <returns>
    ///   <c>true</c> if the specified <see cref="object" /> is equal to this instance; otherwise, <c>false</c>.
    /// </returns>
    public override bool Equals(object? other) => other is HashCode code && Equals(code);

    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <param name="other">An object to compare with this object.</param>
    /// <returns>
    /// true if the current object is equal to the <paramref name="other">other</paramref> parameter; otherwise, false.
    /// </returns>
    public bool Equals(HashCode other) => _hashCode == other._hashCode;


    /// <summary>
    /// Performs an implicit conversion from <see cref="HashCode"/> to <see cref="int"/>.
    /// </summary>
    /// <param name="hashCode">The hash code.</param>
    /// <returns>
    /// The result of the conversion.
    /// </returns>
    public static implicit operator int(HashCode hashCode) => hashCode._hashCode;

    /// <summary>
    /// Compares two values to determine equality.
    /// </summary>
    /// <param name="left">The value to compare with right.</param>
    /// <param name="right">The value to compare with left.</param>
    /// <returns>
    /// true if left is equal to right; otherwise, false.
    /// </returns>
    public static bool operator ==(HashCode left, HashCode right) => left.Equals(right);

    /// <summary>
    /// Compares two values to determine inequality.
    /// </summary>
    /// <param name="left">The value to compare with right.</param>
    /// <param name="right">The value to compare with left.</param>
    /// <returns>
    /// true if left is not equal to right; otherwise, false.
    /// </returns>
    public static bool operator !=(HashCode left, HashCode right) => !(left == right);


    /// <summary>
    /// Deterministic string hash function
    /// </summary>
    /// <param name="text">The text to hash.</param>
    /// <returns>A 32-bit signed integer hash code.</returns>
    public static int HashString(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        int hash = Seed;

        unchecked
        {
            // ReSharper disable once LoopCanBeConvertedToQuery
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var index = 0; index < text!.Length; index++)
                hash = (hash * Multiplier) + text[index];

        }

        return hash;
    }
}
