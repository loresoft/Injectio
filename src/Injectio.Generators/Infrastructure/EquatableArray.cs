using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Injectio.Generators.Infrastructure;


/// <summary>
/// Factory for creating <see cref="EquatableArray{T}"/> instances from spans.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class EquatableArray
{
    /// <summary>
    /// Creates an <see cref="EquatableArray{T}"/> from a <see cref="ReadOnlySpan{T}"/>.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="items">The span of items to copy.</param>
    /// <returns>A new <see cref="EquatableArray{T}"/> containing the items.</returns>
    public static EquatableArray<T> Create<T>(ReadOnlySpan<T> items)
        where T : IEquatable<T>
        => new(items.ToArray());
}

/// <summary>
/// An immutable array wrapper that implements value-based equality, suitable for use
/// in incremental generator models where structural comparison is required.
/// </summary>
/// <typeparam name="T">The element type, which must implement <see cref="IEquatable{T}"/>.</typeparam>
[ExcludeFromCodeCoverage]
[CollectionBuilder(typeof(EquatableArray), nameof(EquatableArray.Create))]
public readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T>
    where T : IEquatable<T>
{
    /// <summary>
    /// An empty <see cref="EquatableArray{T}"/> instance.
    /// </summary>
    public static readonly EquatableArray<T> Empty = new();


    /// <summary>
    /// Initializes a new empty <see cref="EquatableArray{T}"/>.
    /// </summary>
    public EquatableArray() : this([]) { }

    /// <summary>
    /// Initializes a new <see cref="EquatableArray{T}"/> from an array.
    /// </summary>
    /// <param name="array">The array to wrap.</param>
    public EquatableArray(T[] array) => Array = array ?? [];

    /// <summary>
    /// Initializes a new <see cref="EquatableArray{T}"/> from an enumerable.
    /// </summary>
    /// <param name="items">The items to copy into the array.</param>
    public EquatableArray(IEnumerable<T> items) => Array = items.ToArray() ?? [];


    /// <summary>
    /// Gets the underlying array.
    /// </summary>
    public T[] Array { get; }

    /// <summary>
    /// Gets the number of elements in the array.
    /// </summary>
    public int Count => Array.Length;


    /// <summary>
    /// Returns the array as a <see cref="ReadOnlySpan{T}"/>.
    /// </summary>
    /// <returns>A read-only span over the array elements.</returns>
    public ReadOnlySpan<T> AsSpan() => Array.AsSpan();

    /// <summary>
    /// Returns the underlying array.
    /// </summary>
    /// <returns>The underlying array.</returns>
    public T[] AsArray() => Array;


    /// <inheritdoc />
    public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right) => left.Equals(right);

    /// <inheritdoc />
    public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right) => !left.Equals(right);

    /// <inheritdoc />
    public bool Equals(EquatableArray<T> array) => Array.AsSpan().SequenceEqual(array.AsSpan());

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is EquatableArray<T> array && Equals(this, array);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        if (Array is not T[] array)
            return 0;

        var hashCode = 16777619;

        for (int i = 0; i < array.Length; i++)
            hashCode = unchecked((hashCode * -1521134295) + EqualityComparer<T>.Default.GetHashCode(array[i]));

        return hashCode;
    }


    /// <inheritdoc />
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => (Array as IEnumerable<T>).GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => Array.GetEnumerator();


    /// <summary>
    /// Implicitly converts an array to an <see cref="EquatableArray{T}"/>.
    /// </summary>
    /// <param name="array">The array to wrap.</param>
    public static implicit operator EquatableArray<T>(T[] array) => new(array);

    /// <summary>
    /// Implicitly converts a list to an <see cref="EquatableArray{T}"/>.
    /// </summary>
    /// <param name="items">The list to copy.</param>
    public static implicit operator EquatableArray<T>(List<T> items) => new(items);
}
