namespace NexusMods.DataModel.Interprocess;

public interface IMessage
{
    /// <summary>
    /// Maximum size of the message. Must be greater than 16 bytes. Internally enough buffer space
    /// will be allocated to hold 16 messages.
    /// </summary>
    public static abstract int MaxSize { get; }
    
    /// <summary>
    /// Writes the message to the buffer.
    /// </summary>
    /// <param name="buffer">Buffer that will be MaxSize in length</param>
    /// <returns>The number of bytes written to the buffer</returns>
    public int Write(Span<byte> buffer);

    /// <summary>
    /// Reads the message from the buffer.
    /// </summary>
    /// <param name="buffer">Buffer that will be MaxSize in length</param>
    /// <returns></returns>
    public static abstract IMessage Read(ReadOnlySpan<byte> buffer);
}