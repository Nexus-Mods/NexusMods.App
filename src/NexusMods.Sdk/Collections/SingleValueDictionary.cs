using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace NexusMods.Sdk.Collections;

/// <summary>
/// Read-only dictionary for a single value.
/// </summary>
[PublicAPI]
[DebuggerDisplay("Key = {_kv.Key} Value = {_kv.Value}")]
public class SingleValueDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
{
    private readonly KeyValuePair<TKey, TValue> _kv;
    private readonly IEqualityComparer<TKey> _keyEqualityComparer;

    /// <summary>
    /// Constructor.
    /// </summary>
    public SingleValueDictionary(KeyValuePair<TKey, TValue> kv, IEqualityComparer<TKey>? keyEqualityComparer = null)
    { 
        _kv = kv;
        _keyEqualityComparer = keyEqualityComparer ?? EqualityComparer<TKey>.Default;
    }

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => new SingleValueEnumerator<KeyValuePair<TKey, TValue>>(_kv);
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc/>
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        if (!_keyEqualityComparer.Equals(key, _kv.Key))
        {
            value = default(TValue);
            return false;
        }

        value = _kv.Value;
        return true;
    }

    /// <inheritdoc/>
    public int Count => 1;

    /// <inheritdoc/>
    public bool ContainsKey(TKey key) => _keyEqualityComparer.Equals(key, _kv.Key);

    /// <inheritdoc/>
    public TValue this[TKey key] => _keyEqualityComparer.Equals(key, _kv.Key) ? _kv.Value : throw new IndexOutOfRangeException();

    /// <inheritdoc/>
    public IEnumerable<TKey> Keys => new SingleValueEnumerable<TKey>(_kv.Key);

    /// <inheritdoc/>
    public IEnumerable<TValue> Values => new SingleValueEnumerable<TValue>(_kv.Value);
}
