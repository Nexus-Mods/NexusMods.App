using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
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
    /// Decodes a Nexus Mods API result which contains an 'uid' field into a <see cref="UidForFile"/>.
    /// </summary>
    /// <param name="uid">The 'uid' field of a GraphQL API query. This should be an 8 byte number represented as a string.</param>
    /// <remarks>
    /// This throws if <param name="uid"/> is not a valid number.
    /// </remarks>
    public static UidForMod FromV2Api(string uid) => FromUlong(ulong.Parse(uid));

    /// <summary>
    /// Reinterprets the current <see cref="UidForMod"/> as a single <see cref="ulong"/>.
    /// </summary>
    public ulong AsUlong => Unsafe.As<UidForMod, ulong>(ref this);
    
    /// <summary>
    /// Reinterprets a given <see cref="ulong"/> into a <see cref="UidForMod"/>.
    /// </summary>
    public static UidForMod FromUlong(ulong value) => Unsafe.As<ulong, UidForMod>(ref value);
}

/// <summary>
/// Mod ID attribute, for NexusMods API mod IDs.
/// </summary>
public class UidForModAttribute(string ns, string name) 
    : ScalarAttribute<UidForMod, ulong>(ValueTags.UInt64, ns, name)
{
    /// <inheritdoc />
    protected override ulong ToLowLevel(UidForMod value) => value.AsUlong;

    /// <inheritdoc />
    protected override UidForMod FromLowLevel(ulong value, ValueTags tags, RegistryId registryId) => UidForMod.FromUlong(value);
} 
