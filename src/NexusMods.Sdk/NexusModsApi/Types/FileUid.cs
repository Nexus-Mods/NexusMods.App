using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.Sdk.NexusModsApi;

/// <summary>
/// This represents a unique ID of an individual file as stored on Nexus Mods.
/// 
/// This is a composite key of <see cref="FileId"/> and <see cref="GameId"/>, where
/// the upper 4 bytes represent the <see cref="FileId"/> and the lower 4 bytes represent
/// the <see cref="GameId"/>.
///
/// This is consistent with how the Nexus Mods backend produces the UID and is not
/// expected to change.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct FileUid : IEquatable<FileUid>
{
    /// <summary>
    /// Unique identifier for the file, within the specific <see cref="GameId"/>.
    /// </summary>
    public readonly FileId FileId;

    /// <summary>
    /// Unique identifier for the game.
    /// </summary>
    public readonly GameId GameId;

    /// <summary>
    /// Constructor.
    /// </summary>
    public FileUid(FileId fileId, GameId gameId)
    {
        FileId = fileId;
        GameId = gameId;
    }

    /// <summary>
    /// Decodes a Nexus Mods API result which contains an 'uid' field into a <see cref="FileUid"/>.
    /// </summary>
    /// <param name="uid">The 'uid' field of a GraphQL API query. This should be an 8 byte number represented as a string.</param>
    /// <remarks>
    /// This throws if <param name="uid"/> is not a valid number.
    /// </remarks>
    public static FileUid FromV2Api(string uid) => FromUlong(ulong.Parse(uid));

    /// <summary>
    /// Converts the UID to a string accepted by the V2 API.
    /// </summary>
    public string ToV2Api() => AsUlong.ToString();

    /// <summary>
    /// Reinterprets the current <see cref="FileUid"/> as a single <see cref="ulong"/>.
    /// </summary>
    public ulong AsUlong => Unsafe.As<FileUid, ulong>(ref Unsafe.AsRef(in this));

    /// <summary>
    /// Reinterprets a given <see cref="ulong"/> into a <see cref="FileUid"/>.
    /// </summary>
    public static FileUid FromUlong(ulong value) => Unsafe.As<ulong, FileUid>(ref value);

    /// <inheritdoc/>
    public bool Equals(FileUid other)
    {
        return FileId.Equals(other.FileId) && GameId.Equals(other.GameId);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is FileUid other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        unchecked
        {
            return (FileId.GetHashCode() * 397) ^ GameId.GetHashCode();
        }
    }

    /// <summary>
    /// Equality.
    /// </summary>
    public static bool operator ==(FileUid left, FileUid right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Inequality.
    /// </summary>
    public static bool operator !=(FileUid left, FileUid right)
    {
        return !(left == right);
    }
}

/// <summary>
/// Attribute for <see cref="FileUid"/>.
/// </summary>
public class FileUidAttribute(string ns, string name) : ScalarAttribute<FileUid, ulong, UInt64Serializer>(ns, name)
{
    /// <inheritdoc />
    public override ulong ToLowLevel(FileUid value) => value.AsUlong;

    /// <inheritdoc />
    public override FileUid FromLowLevel(ulong value, AttributeResolver resolver) => FileUid.FromUlong(value);
} 
