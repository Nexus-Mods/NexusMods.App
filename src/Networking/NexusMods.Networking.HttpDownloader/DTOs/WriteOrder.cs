namespace NexusMods.Networking.HttpDownloader.DTOs;

/// <summary>
/// Used to contain information that will be sent to the write queue.
/// </summary>
struct WriteOrder
{
    public long Offset;
    public int Size;
    public byte[] Data;
}