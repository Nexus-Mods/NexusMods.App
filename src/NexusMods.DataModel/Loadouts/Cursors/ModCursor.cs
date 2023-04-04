using NexusMods.DataModel.Interprocess;
using NexusMods.Paths.Extensions;

namespace NexusMods.DataModel.Loadouts.Cursors;

/// <summary>
/// Groups a <see cref="LoadoutId"/> and a <see cref="ModId"/> together, for
/// easy passing around as a single object.
/// </summary>
/// <param name="LoadoutId"></param>
/// <param name="ModId"></param>
public readonly record struct ModCursor(LoadoutId LoadoutId, ModId ModId) : IMessage
{
    /// <summary>
    /// Two guids, 16 bytes each
    /// </summary>
    public static int MaxSize => 32;

    public int Write(Span<byte> buffer)
    {
        LoadoutId.Value.TryWriteBytes(buffer);
        ModId.Value.TryWriteBytes(buffer.SliceFast(16));
        return MaxSize;
    }

    public static IMessage Read(ReadOnlySpan<byte> buffer)
    {
        var loadoutId = LoadoutId.From(buffer.SliceFast(0, 16));
        var modId = ModId.From(new Guid(buffer.SliceFast(16, 16)));
        return new ModCursor(loadoutId, modId);
    }
}
