using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.Sdk.NexusModsApi;

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
public readonly struct ModUid : IEquatable<ModUid>
{
    /// <summary>
    /// Unique identifier for the mod, within the specific <see cref="GameId"/>.
    /// </summary>
    public readonly ModId ModId;

    /// <summary>
    /// Unique identifier for the game.
    /// </summary>
    public readonly GameId GameId;

    /// <summary>
    /// Constructor.
    /// </summary>
    public ModUid(ModId modId, GameId gameId)
    {
        ModId = modId;
        GameId = gameId;
    }

    /// <summary>
    /// Decodes a Nexus Mods API result which contains an 'uid' field into a <see cref="FileUid"/>.
    /// </summary>
    /// <param name="uid">The 'uid' field of a GraphQL API query. This should be an 8 byte number represented as a string.</param>
    /// <remarks>
    /// This throws if <paramref name="uid"/> is not a valid number.
    /// </remarks>
    public static ModUid FromV2Api(string uid) => FromUlong(ulong.Parse(uid));
    
    /// <summary>
    /// Converts the UID to a string accepted by the V2 API.
    /// </summary>
    public string ToV2Api() => AsUlong.ToString();

    /// <summary>
    /// Reinterprets the current <see cref="ModUid"/> as a single <see cref="ulong"/>.
    /// </summary>
    public ulong AsUlong => Unsafe.As<ModUid, ulong>(ref Unsafe.AsRef(in this));

    /// <summary>
    /// Reinterprets a given <see cref="ulong"/> into a <see cref="ModUid"/>.
    /// </summary>
    public static ModUid FromUlong(ulong value) => Unsafe.As<ulong, ModUid>(ref value);

    /// <inheritdoc/>
    public bool Equals(ModUid other)
    {
        return ModId.Equals(other.ModId) && GameId.Equals(other.GameId);
    }

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is ModUid other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        unchecked
        {
            return (ModId.GetHashCode() * 397) ^ GameId.GetHashCode();
        }
    }

    /// <summary>
    /// Equality.
    /// </summary>
    public static bool operator ==(ModUid left, ModUid right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Inequality.
    /// </summary>
    public static bool operator !=(ModUid left, ModUid right)
    {
        return !(left == right);
    }
}

/// <summary>
/// Attribute for <see cref="ModUid"/>.
/// </summary>
public class ModUidAttribute(string ns, string name) : ScalarAttribute<ModUid, ulong, UInt64Serializer>(ns, name)
{
    /// <inheritdoc />
    protected override ulong ToLowLevel(ModUid value) => value.AsUlong;

    /// <inheritdoc />
    protected override ModUid FromLowLevel(ulong value, AttributeResolver resolver) => ModUid.FromUlong(value);
} 
