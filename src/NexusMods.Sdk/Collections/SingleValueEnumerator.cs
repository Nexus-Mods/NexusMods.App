using System.Collections;
using System.Diagnostics;
using JetBrains.Annotations;

namespace NexusMods.Sdk.Collections;

/// <summary>
/// Implements <see cref="IEnumerator{T}"/> for a single value.
/// </summary>
[PublicAPI]
[DebuggerDisplay("Value = {Current} Accessed={_didAccess}")]
public sealed class SingleValueEnumerator<TValue> : IEnumerator<TValue>
{
    private bool _didAccess;

    /// <summary>
    /// Constructor.
    /// </summary>
    public SingleValueEnumerator(TValue value)
    {
        Current = value;
    }

    /// <inheritdoc/>
    public bool MoveNext()
    {
        if (_didAccess) return false;
        _didAccess = true;
        return true;
    }

    /// <inheritdoc/>
    public void Reset()
    {
        _didAccess = false;
    }

    /// <inheritdoc/>
    public TValue Current { get; }
    object? IEnumerator.Current => Current;

    /// <inheritdoc/>
    public void Dispose() { }
}
