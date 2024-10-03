using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
namespace NexusMods.Networking.ModUpdates.Structures;

/// <summary>
/// This represents a unique ID of an individual mod page as stored on Nexus Mods.
/// </summary>
/// <remarks>
///     Not tested on Big Endian architectures.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Uid
{
    /// <summary>
    /// Unique identifier for the mod, within the specific <see cref="GameId"/>.
    /// </summary>
    public ModId ModId;

    /// <summary>
    /// Unique identifier for the game.
    /// </summary>
    public GameId GameId;

    /// <summary>
    /// Reinterprets the current <see cref="Uid"/> as a single <see cref="ulong"/>.
    /// </summary>
    public ulong AsUlong => Unsafe.As<Uid, ulong>(ref this);
    
    /// <summary>
    /// Reinterprets a given <see cref="ulong"/> into a <see cref="Uid"/>.
    /// </summary>
    public static Uid FromUlong(ulong value) => Unsafe.As<ulong, Uid>(ref value);
}
