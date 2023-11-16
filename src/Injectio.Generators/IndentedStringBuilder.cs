using System.Text;

namespace Injectio.Generators;

/// <summary>
/// A thin wrapper over <see cref="StringBuilder" /> that adds indentation to each line built.
/// </summary>
public class IndentedStringBuilder
{
    private const byte IndentSize = 4;
    private byte _indent;
    private bool _indentPending = true;

    private readonly StringBuilder _stringBuilder = new();

    /// <summary>
    /// The current length of the built string.
    /// </summary>
    public virtual int Length
        => _stringBuilder.Length;

    /// <summary>
    /// Appends the current indent and then the given string to the string being built.
    /// </summary>
    /// <param name="value">The string to append.</param>
    /// <returns>This builder so that additional calls can be chained.</returns>
    public virtual IndentedStringBuilder Append(string value)
    {
        DoIndent();

        _stringBuilder.Append(value);

        return this;
    }

    /// <summary>
    /// Appends the current indent and then the given string to the string being built.
    /// </summary>
    /// <param name="value">The value to append.</param>
    /// <returns>This builder so that additional calls can be chained.</returns>
    public virtual IndentedStringBuilder Append<T>(T value)
    {
        if (value == null)
            return this;

        DoIndent();

        _stringBuilder.Append(value.ToString());

        return this;
    }

    /// <summary>
    /// Appends the current indent and then the given char to the string being built.
    /// </summary>
    /// <param name="value">The char to append.</param>
    /// <returns>This builder so that additional calls can be chained.</returns>
    public virtual IndentedStringBuilder Append(char value)
    {
        DoIndent();

        _stringBuilder.Append(value);

        return this;
    }

    /// <summary>
    /// Appends the current indent and then the given strings to the string being built.
    /// </summary>
    /// <param name="value">The strings to append.</param>
    /// <returns>This builder so that additional calls can be chained.</returns>
    public virtual IndentedStringBuilder Append(IEnumerable<string> value)
    {
        DoIndent();

        foreach (var str in value)
            _stringBuilder.Append(str);

        return this;
    }

    /// <summary>
    ///     Appends the current indent and then the given chars to the string being built.
    /// </summary>
    /// <param name="value">The chars to append.</param>
    /// <returns>This builder so that additional calls can be chained.</returns>
    public virtual IndentedStringBuilder Append(IEnumerable<char> value)
    {
        DoIndent();

        foreach (var chr in value)
            _stringBuilder.Append(chr);

        return this;
    }

    /// <summary>
    /// Appends the current indent and then the given strings to the string being built.
    /// </summary>
    /// <typeparam name="T">The type of the members of values.</typeparam>
    /// <param name="separator">The string to use as a separator. separator is included in the concatenated and appended strings only if values has more than one element.</param>
    /// <param name="values">A collection that contains the objects to concatenate and append to the current instance of the string builder.</param>
    /// <returns>This builder so that additional calls can be chained.</returns>
    public virtual IndentedStringBuilder AppendJoin<T>(IEnumerable<T> values, string separator)
    {
        if (values is null)
            throw new ArgumentNullException(nameof(values));

        separator ??= string.Empty;

        DoIndent();

        var wroteValue = false;

        foreach (var value in values)
        {
            if (wroteValue)
                _stringBuilder.Append(separator);

            _stringBuilder.Append(value);
            wroteValue = true;
        }

        return this;
    }

    /// <summary>Appends the current indent and then the given strings to the string being built.</summary>
    /// <typeparam name="T">The type of the members of values.</typeparam>
    /// <param name="values">A collection that contains the objects to concatenate and append to the current instance of the string builder.</param>
    /// <param name="separator">The string to use as a separator. separator is included in the concatenated and appended strings only if values has more than one element.</param>
    /// <param name="condition"></param>
    /// <returns>This builder so that additional calls can be chained.</returns>
    public virtual IndentedStringBuilder AppendJoinIf<T>(IEnumerable<T> values, string separator, Func<IEnumerable<T>, bool>? condition = null)
    {
        var c = condition ?? (s => s.Any());

        if (c(values))
            AppendJoin(values, separator);

        return this;
    }

    /// <summary>Appends the current indent and then the given strings to the string being built.</summary>
    /// <typeparam name="T">The type of the members of values.</typeparam>
    /// <param name="values">A collection that contains the objects to concatenate and append to the current instance of the string builder.</param>
    /// <param name="separator">The string to use as a separator. separator is included in the concatenated and appended strings only if values has more than one element.</param>
    /// <param name="condition"></param>
    /// <returns>This builder so that additional calls can be chained.</returns>
    public virtual IndentedStringBuilder AppendJoinIf<T>(IEnumerable<T> values, string separator, bool condition)
    {
        if (condition)
            AppendJoin(values, separator);

        return this;
    }

    /// <summary>
    ///     Appends a new line to the string being built.
    /// </summary>
    /// <returns>This builder so that additional calls can be chained.</returns>
    public virtual IndentedStringBuilder AppendLine()
    {
        AppendLine(string.Empty);

        return this;
    }

    /// <summary>
    ///     <para>
    ///         Appends the current indent, the given string, and a new line to the string being built.
    ///     </para>
    ///     <para>
    ///         If the given string itself contains a new line, the part of the string after that new line will not be indented.
    ///     </para>
    /// </summary>
    /// <param name="value">The string to append.</param>
    /// <returns>This builder so that additional calls can be chained.</returns>
    public virtual IndentedStringBuilder AppendLine(string value)
    {
        if (value.Length != 0)
            DoIndent();

        _stringBuilder.AppendLine(value);

        _indentPending = true;

        return this;
    }

    /// <summary>
    ///     Separates the given string into lines, and then appends each line, prefixed
    ///     by the current indent and followed by a new line, to the string being built.
    /// </summary>
    /// <param name="value">The string to append.</param>
    /// <param name="skipFinalNewline">If <see langword="true" />, then the terminating new line is not added after the last line.</param>
    /// <returns>This builder so that additional calls can be chained.</returns>
    public virtual IndentedStringBuilder AppendLines(string value, bool skipFinalNewline = false)
    {
        using (var reader = new StringReader(value))
        {
            var first = true;
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (first)
                    first = false;
                else
                    AppendLine();

                if (line.Length != 0)
                    Append(line);
            }
        }

        if (!skipFinalNewline)
            AppendLine();

        return this;
    }

    /// <summary>
    /// Appends a copy of the specified string if <paramref name="condition"/> is met.
    /// </summary>
    /// <param name="text">The string to append.</param>
    /// <param name="condition">The condition delegate to evaluate. If condition is null, String.IsNullOrWhiteSpace method will be used.</param>
    public IndentedStringBuilder AppendIf(string text, Func<string, bool>? condition = null)
    {
        var c = condition ?? (s => !string.IsNullOrEmpty(s));

        if (c(text))
            Append(text);

        return this;
    }

    /// <summary>
    /// Appends a copy of the specified string if <paramref name="condition"/> is met.
    /// </summary>
    /// <param name="text">The string to append.</param>
    /// <param name="condition">The condition delegate to evaluate. If condition is null, String.IsNullOrWhiteSpace method will be used.</param>
    public IndentedStringBuilder AppendIf(string text, bool condition)
    {
        if (condition)
            Append(text);

        return this;
    }

    /// <summary>
    /// Appends a copy of the specified string followed by the default line terminator if <paramref name="condition"/> is met.
    /// </summary>
    /// <param name="text">The string to append.</param>
    /// <param name="condition">The condition delegate to evaluate. If condition is null, String.IsNullOrWhiteSpace method will be used.</param>
    public IndentedStringBuilder AppendLineIf(string text, Func<string, bool>? condition = null)
    {
        var c = condition ?? (s => !string.IsNullOrEmpty(s));

        if (c(text))
            AppendLine(text);

        return this;
    }

    /// <summary>
    ///     Resets this builder ready to build a new string.
    /// </summary>
    /// <returns>This builder so that additional calls can be chained.</returns>
    public virtual IndentedStringBuilder Clear()
    {
        _stringBuilder.Clear();
        _indent = 0;

        return this;
    }

    /// <summary>
    ///     Increments the indent.
    /// </summary>
    /// <returns>This builder so that additional calls can be chained.</returns>
    public virtual IndentedStringBuilder IncrementIndent()
    {
        _indent++;

        return this;
    }

    /// <summary>
    ///     Decrements the indent.
    /// </summary>
    /// <returns>This builder so that additional calls can be chained.</returns>
    public virtual IndentedStringBuilder DecrementIndent()
    {
        if (_indent > 0)
            _indent--;

        return this;
    }

    /// <summary>
    ///     Creates a scoped indenter that will increment the indent, then decrement it when disposed.
    /// </summary>
    /// <returns>An indenter.</returns>
    public virtual IDisposable Indent()
        => new Indenter(this);

    /// <summary>
    ///     Temporarily disables all indentation. Restores the original indentation when the returned object is disposed.
    /// </summary>
    /// <returns>An object that restores the original indentation when disposed.</returns>
    public virtual IDisposable SuspendIndent()
        => new IndentSuspender(this);

    /// <summary>
    ///     Returns the built string.
    /// </summary>
    /// <returns>The built string.</returns>
    public override string ToString()
        => _stringBuilder.ToString();

    private void DoIndent()
    {
        if (_indentPending && _indent > 0)
            _stringBuilder.Append(' ', _indent * IndentSize);

        _indentPending = false;
    }

    private sealed class Indenter : IDisposable
    {
        private readonly IndentedStringBuilder _stringBuilder;

        public Indenter(IndentedStringBuilder stringBuilder)
        {
            _stringBuilder = stringBuilder;

            _stringBuilder.IncrementIndent();
        }

        public void Dispose()
            => _stringBuilder.DecrementIndent();
    }

    private sealed class IndentSuspender : IDisposable
    {
        private readonly IndentedStringBuilder _stringBuilder;
        private readonly byte _indent;

        public IndentSuspender(IndentedStringBuilder stringBuilder)
        {
            _stringBuilder = stringBuilder;
            _indent = _stringBuilder._indent;
            _stringBuilder._indent = 0;
        }

        public void Dispose()
            => _stringBuilder._indent = _indent;
    }
}
