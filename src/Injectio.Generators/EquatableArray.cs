using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Injectio.Generators;

[ExcludeFromCodeCoverage]
public readonly struct EquatableArray<T>(T[] array) : IEquatable<EquatableArray<T>>, IEnumerable<T>
    where T : IEquatable<T>
{
    public T[] Array { get; } = array ?? [];

    public int Count => Array.Length;

    public ReadOnlySpan<T> AsSpan() => Array.AsSpan();

    public T[] AsArray() => Array;


    public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right) => left.Equals(right);

    public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right) => !left.Equals(right);

    public bool Equals(EquatableArray<T> array) => Array.AsSpan().SequenceEqual(array.AsSpan());

    public override bool Equals(object? obj) => obj is EquatableArray<T> array && Equals(this, array);

    public override int GetHashCode()
    {
        if (Array is not T[] array)
            return 0;

        var hashCode = 16777619;

        for (int i = 0; i < array.Length; i++)
            hashCode = unchecked((hashCode * -1521134295) + EqualityComparer<T>.Default.GetHashCode(array[i]));

        return hashCode;
    }


    IEnumerator<T> IEnumerable<T>.GetEnumerator() => (Array as IEnumerable<T>).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => Array.GetEnumerator();


    public static implicit operator EquatableArray<T>(T[] array) => new(array);
}
