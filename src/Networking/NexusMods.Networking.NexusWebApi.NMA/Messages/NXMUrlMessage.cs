using System.Text;
using NexusMods.DataModel.Interprocess;
using NexusMods.Networking.NexusWebApi.Types;

namespace NexusMods.Networking.NexusWebApi.NMA.Messages;

/// <summary>
/// nxm url message used in IPC. The oauth callback will spawn a new instance of NMA
/// that then needs to send the token back to the "main" process that made the request
/// </summary>
// ReSharper disable once InconsistentNaming
public readonly struct NXMUrlMessage : IMessage
{
    /// <summary>
    /// the actual url
    /// </summary>
    public NXMUrl Value { get; init; }

    /// <inheritdoc/>
    public static int MaxSize => 16 * 1024;

    /// <inheritdoc/>
    public static IMessage Read(ReadOnlySpan<byte> buffer)
    {
        var value = Encoding.UTF8.GetString(buffer);
        return new NXMUrlMessage { Value = NXMUrl.Parse(new Uri(value)) };
    }

    /// <inheritdoc/>
    public int Write(Span<byte> buffer)
    {
        var buf = Encoding.UTF8.GetBytes(Value.ToString()!);
        buf.CopyTo(buffer);
        return buf.Length;
    }
}
