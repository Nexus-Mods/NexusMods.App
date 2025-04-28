using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

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
public struct UidForMod : IEquatable<UidForMod>
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
    /// This throws if <paramref name="uid"/> is not a valid number.
    /// </remarks>
    public static UidForMod FromV2Api(string uid) => FromUlong(ulong.Parse(uid));
    
    /// <summary>
    /// Converts the UID to a string accepted by the V2 API.
    /// </summary>
    public string ToV2Api() => AsUlong.ToString();

    /// <summary>
    /// Reinterprets the current <see cref="UidForMod"/> as a single <see cref="ulong"/>.
    /// </summary>
    public ulong AsUlong => Unsafe.As<UidForMod, ulong>(ref this);
    
    /// <summary>
    /// Reinterprets a given <see cref="ulong"/> into a <see cref="UidForMod"/>.
    /// </summary>
    public static UidForMod FromUlong(ulong value) => Unsafe.As<ulong, UidForMod>(ref value);

    /// <inheritdoc/>
    public bool Equals(UidForMod other)
    {
        return ModId.Equals(other.ModId) && GameId.Equals(other.GameId);
    }

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is UidForMod other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        unchecked
        {
            return (ModId.GetHashCode() * 397) ^ GameId.GetHashCode();
        }
    }
}

/// <summary>
/// Attribute that uniquely identifies a mod on Nexus Mods.
/// See <see cref="UidForMod"/> for more details.
/// </summary>
public class UidForModAttribute(string ns, string name) 
    : ScalarAttribute<UidForMod, ulong, UInt64Serializer>(ns, name)
{
    /// <inheritdoc />
    protected override ulong ToLowLevel(UidForMod value) => value.AsUlong;

    /// <inheritdoc />
    protected override UidForMod FromLowLevel(ulong value, AttributeResolver resolver) => UidForMod.FromUlong(value);
} 
