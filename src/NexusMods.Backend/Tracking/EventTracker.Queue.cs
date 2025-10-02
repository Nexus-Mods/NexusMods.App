using System.Diagnostics;
using System.Text;
using CommunityToolkit.HighPerformance.Buffers;
using Microsoft.Extensions.Logging;

namespace NexusMods.Backend.Tracking;

internal partial class EventTracker
{
    // NOTE(erri120): Arbitrarily chosen limit for our client.
    // The Mixpanel limits are very high: 2000 events per request
    // One client instance shouldn't need more than 128 events in one batch
    private const int MaxEvents = 128;
    private readonly PreparedEvent[] _insertRingBuffer = new PreparedEvent[MaxEvents];
    private readonly PreparedEvent[] _sortedReadingCopy = new PreparedEvent[MaxEvents];

    private ulong _lastGeneratedId;
    private ulong _highestSeenId;

    private void InsertEvent(string name, ArrayPoolBufferWriter<byte> bufferWriter)
    {
        var id = Interlocked.Increment(ref _lastGeneratedId);
        var newIndex = (int)(id % MaxEvents);
        Debug.Assert(newIndex >= 0);
        Debug.Assert(newIndex < _insertRingBuffer.Length);

        _insertRingBuffer[newIndex] = new PreparedEvent(id, name, bufferWriter);
    }

    private void FinalizeEvent(string name, ArrayPoolBufferWriter<byte> bufferWriter)
    {
        // https://developer.mixpanel.com/reference/import-events#high-level-requirements
        // "Each event must be smaller than 1MB of uncompressed JSON."
        // NOTE(erri120): unclear whether it's 1MB (1000 * 1000 bytes) or 1MiB (1024 * 1024) so I'm using the smaller
        const int limit = 1000 * 1000;

        if (bufferWriter.WrittenCount > limit)
        {
            _logger.LogDebug("Event validation for `{EventName}` failed because the event payload has a size of `{Size}` and is above the limit of `{Limit}` bytes", name, bufferWriter.WrittenCount, limit);
            return;
        }

        InsertEvent(name, bufferWriter);
    }

    [DebuggerDisplay("{Decode()}")]
    private readonly record struct PreparedEvent(ulong Id, string EventName, ArrayPoolBufferWriter<byte> BufferWriter) : IDisposable, IComparable<PreparedEvent>
    {
        public bool IsInitialized => EventName is not null && BufferWriter is not null;

        public int CompareTo(PreparedEvent other) => Id.CompareTo(other.Id);

        public void Dispose()
        {
            if (!IsInitialized) return;
            BufferWriter.Dispose();
        }

        public string Decode() => Encoding.UTF8.GetString(BufferWriter.WrittenSpan);
    }

    public void Track<T0>(string name, (string name, T0 value) property0)
    {
        var bufferWriter = new ArrayPoolBufferWriter<byte>(_arrayPool);
        using (var writer = EventWriter.Create(this, bufferWriter, name))
        {
            writer.Write(property0);
        }

        FinalizeEvent(name, bufferWriter);
    }
    
    public void Track<T0, T1>(string name,
        (string name, T0 value) property0,
        (string name, T1 value) property1)
    {
        var bufferWriter = new ArrayPoolBufferWriter<byte>(_arrayPool);
        using (var writer = EventWriter.Create(this, bufferWriter, name))
        {
            writer.Write(property0);
            writer.Write(property1);
        }

        FinalizeEvent(name, bufferWriter);
    }
    
    public void Track<T0, T1, T2>(string name,
        (string name, T0 value) property0,
        (string name, T1 value) property1,
        (string name, T2 value) property2)
    {
        var bufferWriter = new ArrayPoolBufferWriter<byte>(_arrayPool);
        using (var writer = EventWriter.Create(this, bufferWriter, name))
        {
            writer.Write(property0);
            writer.Write(property1);
            writer.Write(property2);
        }

        FinalizeEvent(name, bufferWriter);
    }
    
    public void Track<T0, T1, T2, T3>(string name,
        (string name, T0 value) property0,
        (string name, T1 value) property1,
        (string name, T2 value) property2,
        (string name, T3 value) property3)
    {
        var bufferWriter = new ArrayPoolBufferWriter<byte>(_arrayPool);
        using (var writer = EventWriter.Create(this, bufferWriter, name))
        {
            writer.Write(property0);
            writer.Write(property1);
            writer.Write(property2);
            writer.Write(property3);
        }

        FinalizeEvent(name, bufferWriter);
    }
    
    public void Track<T0, T1, T2, T3, T4>(string name,
        (string name, T0 value) property0,
        (string name, T1 value) property1,
        (string name, T2 value) property2,
        (string name, T3 value) property3,
        (string name, T4 value) property4)
    {
        var bufferWriter = new ArrayPoolBufferWriter<byte>(_arrayPool);
        using (var writer = EventWriter.Create(this, bufferWriter, name))
        {
            writer.Write(property0);
            writer.Write(property1);
            writer.Write(property2);
            writer.Write(property3);
            writer.Write(property4);
        }

        FinalizeEvent(name, bufferWriter);
    }

    public void Track<T0, T1, T2, T3, T4, T5>(string name,
        (string name, T0 value) property0,
        (string name, T1 value) property1,
        (string name, T2 value) property2,
        (string name, T3 value) property3,
        (string name, T4 value) property4,
        (string name, T5 value) property5)
    {
        var bufferWriter = new ArrayPoolBufferWriter<byte>(_arrayPool);
        using (var writer = EventWriter.Create(this, bufferWriter, name))
        {
            writer.Write(property0);
            writer.Write(property1);
            writer.Write(property2);
            writer.Write(property3);
            writer.Write(property4);
            writer.Write(property5);
        }

        FinalizeEvent(name, bufferWriter);
    }
    
    public void Track<T0, T1, T2, T3, T4, T5, T6>(string name,
        (string name, T0 value) property0,
        (string name, T1 value) property1,
        (string name, T2 value) property2,
        (string name, T3 value) property3,
        (string name, T4 value) property4,
        (string name, T5 value) property5,
        (string name, T6 value) property6)
    {
        var bufferWriter = new ArrayPoolBufferWriter<byte>(_arrayPool);
        using (var writer = EventWriter.Create(this, bufferWriter, name))
        {
            writer.Write(property0);
            writer.Write(property1);
            writer.Write(property2);
            writer.Write(property3);
            writer.Write(property4);
            writer.Write(property5);
            writer.Write(property6);
        }

        FinalizeEvent(name, bufferWriter);
    }
    
    public void Track<T0, T1, T2, T3, T4, T5, T6, T7>(string name,
        (string name, T0 value) property0,
        (string name, T1 value) property1,
        (string name, T2 value) property2,
        (string name, T3 value) property3,
        (string name, T4 value) property4,
        (string name, T5 value) property5,
        (string name, T6 value) property6,
        (string name, T7 value) property7)
    {
        var bufferWriter = new ArrayPoolBufferWriter<byte>(_arrayPool);
        using (var writer = EventWriter.Create(this, bufferWriter, name))
        {
            writer.Write(property0);
            writer.Write(property1);
            writer.Write(property2);
            writer.Write(property3);
            writer.Write(property4);
            writer.Write(property5);
            writer.Write(property6);
            writer.Write(property7);
        }

        FinalizeEvent(name, bufferWriter);
    }
    
    public void Track<T0, T1, T2, T3, T4, T5, T6, T7, T8>(string name,
        (string name, T0 value) property0,
        (string name, T1 value) property1,
        (string name, T2 value) property2,
        (string name, T3 value) property3,
        (string name, T4 value) property4,
        (string name, T5 value) property5,
        (string name, T6 value) property6,
        (string name, T7 value) property7,
        (string name, T8 value) property8)
    {
        var bufferWriter = new ArrayPoolBufferWriter<byte>(_arrayPool);
        using (var writer = EventWriter.Create(this, bufferWriter, name))
        {
            writer.Write(property0);
            writer.Write(property1);
            writer.Write(property2);
            writer.Write(property3);
            writer.Write(property4);
            writer.Write(property5);
            writer.Write(property6);
            writer.Write(property7);
            writer.Write(property8);
        }

        FinalizeEvent(name, bufferWriter);
    }

    public void Track<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(string name,
        (string name, T0 value) property0,
        (string name, T1 value) property1,
        (string name, T2 value) property2,
        (string name, T3 value) property3,
        (string name, T4 value) property4,
        (string name, T5 value) property5,
        (string name, T6 value) property6,
        (string name, T7 value) property7,
        (string name, T8 value) property8,
        (string name, T9 value) property9)
    {
        var bufferWriter = new ArrayPoolBufferWriter<byte>(_arrayPool);
        using (var writer = EventWriter.Create(this, bufferWriter, name))
        {
            writer.Write(property0);
            writer.Write(property1);
            writer.Write(property2);
            writer.Write(property3);
            writer.Write(property4);
            writer.Write(property5);
            writer.Write(property6);
            writer.Write(property7);
            writer.Write(property8);
            writer.Write(property9);
        }

        FinalizeEvent(name, bufferWriter);
    }

    public void Track(string name, params ReadOnlySpan<(string name, object value)> properties)
    {
        var bufferWriter = new ArrayPoolBufferWriter<byte>(_arrayPool);
        using (var writer = EventWriter.Create(this, bufferWriter, name))
        {
            foreach (var property in properties)
            {
                writer.Write(property);
            }
        }

        FinalizeEvent(name, bufferWriter);
    }
}
