using Reloaded.Memory.Extensions;

namespace NexusMods.Abstractions.IO.ChunkedStreams;

/// <summary>
/// An extremely lightweight LRU cache that is not thread safe. All values are expected
/// to be unused by the time the next method on this struct is called.
/// </summary>
public struct LightweightLRUCache<TK, TV> : IDisposable
    where TK : IEquatable<TK>
{
    private readonly int _size;
    private readonly TK[] _keys;
    private readonly TV[] _values;
    private int _count;

    /// <summary>
    /// Creates a new cache with the given size.
    /// </summary>
    /// <param name="size"></param>
    public LightweightLRUCache(int size)
    {
        _size = size;
        _keys = new TK[size];
        _values = new TV[size];
        _count = 0;
    }

    /// <summary>
    /// Gets the number of items in the cache.
    /// </summary>
    public int Count => _count;

    /// <summary>
    /// Tries to get a value from the cache. If the value is not in the cache, the value is set to default.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool TryGet(TK key, out TV? value)
    {
        if (Find(key) is var index && index != -1)
        {
            value = _values[index];
            Touch(index);
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Moves the given index to the front of the cache.
    /// </summary>
    /// <param name="index"></param>
    private void Touch(int index)
    {
        if (index == 0) return;
        var keys = _keys.AsSpan();
        var values = _values.AsSpan();

        var key = keys[index];
        var val = values[index];
        keys.SliceFast(0, index).CopyTo(keys.SliceFast(1, index));
        values.SliceFast(0, index).CopyTo(values.SliceFast(1, index));
        keys[0] = key;
        values[0] = val;
    }

    /// <summary>
    /// Gets the index of the given key in the cache, or -1 if the key is not in the cache.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    private readonly int Find(TK key)
    {
        for (var i = 0; i < _count; i++)
        {
            if (_keys[i].Equals(key)) return i;
        }

        return -1;
    }

    /// <summary>
    /// Adds a value to the cache.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="val"></param>
    public void Add(TK key, TV val)
    {
        var keys = _keys.AsSpan();
        var values = _values.AsSpan();

        // Is the cache empty?
        if (_count == 0)
        {
            _keys[0] = key;
            _values[0] = val;
            _count++;
        }
        // Is the key already in the cache?
        else if (Find(key) is var i && i != -1)
        {
            Touch(i);
            var oldVal = values[0];
            values[0] = val;
            (oldVal as IDisposable)?.Dispose();
        }
        // Is the cache full?
        else if (_count == _size)
        {
            // Evict the last item.
            (values[_size - 1] as IDisposable)?.Dispose();
            keys.SliceFast(0, _count - 1).CopyTo(keys.SliceFast(1, _count - 1));
            values.SliceFast(0, _count - 1).CopyTo(values.SliceFast(1, _count - 1));
            keys[0] = key;
            values[0] = val;
        }
        // else add it to the front.
        else
        {
            keys.SliceFast(0, _count).CopyTo(keys.SliceFast(1, _count));
            values.SliceFast(0, _count).CopyTo(values.SliceFast(1, _count));
            _count++;
            keys[0] = key;
            values[0] = val;
        }
    }

    /// <summary>
    /// Disposes all values in the cache.
    /// </summary>
    public void Dispose()
    {
        for (var i = 0; i < _count; i++)
        {
            (_values[i] as IDisposable)?.Dispose();
        }
    }
}
