using System.Collections;
using System.Diagnostics;
using JetBrains.Annotations;

namespace NexusMods.Sdk.Collections;

/// <summary>
/// Implements <see cref="IEnumerable{T}"/> for a single value.
/// </summary>
[PublicAPI]
[DebuggerDisplay("Value = {_value}")]
public sealed class SingleValueEnumerable<TValue> : IEnumerable<TValue>
{
    private readonly TValue _value;

    /// <summary>
    /// Constructor.
    /// </summary>
    public SingleValueEnumerable(TValue value)
    {
        _value = value;
    }

    /// <inheritdoc/>
    public IEnumerator<TValue> GetEnumerator() => new SingleValueEnumerator<TValue>(_value);
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
