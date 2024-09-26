using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
namespace NexusMods.Abstractions.NexusWebApi.Types.V2.Uid;

/// <summary>
/// This represents a unique ID of an individual mod page as stored on Nexus Mods.
/// 
/// This is a composite key of <see cref="ModId"/> and <see cref="GameId"/>, where
/// the upper 4 bytes represent the <see cref="ModId"/> and the lower 4 bytes represent
/// the <see cref="GameId"/>. Values are stored in little endian byte order.
/// 
/// When transferred over the wire via the API, the resulting `ulong` is converted into
/// a string.
///
/// This is consistent with how the Nexus Mods backend produces the UID and is not
/// expected to change.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct UidForMod
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
    /// Reinterprets the current <see cref="UidForMod"/> as a single <see cref="ulong"/>.
    /// </summary>
    public ulong AsUlong => Unsafe.As<UidForMod, ulong>(ref this);
    
    /// <summary>
    /// Reinterprets a given <see cref="ulong"/> into a <see cref="UidForMod"/>.
    /// </summary>
    public static UidForMod FromUlong(ulong value) => Unsafe.As<ulong, UidForMod>(ref value);
}
