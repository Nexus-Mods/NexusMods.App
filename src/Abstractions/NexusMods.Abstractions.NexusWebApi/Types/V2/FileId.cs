using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using TransparentValueObjects;
namespace NexusMods.Abstractions.NexusWebApi.Types.V2;

/// <summary>
/// Unique ID for a game file hosted on a mod page.
///
/// This ID is unique within the context of the game.
/// i.e. This ID might be used for another mod if you search for mods for another game.
/// </summary>
[ValueObject<uint>] // Matches backend. Do not change.
public readonly partial struct FileId : IAugmentWith<DefaultValueAugment>, IAugmentWith<JsonAugment>
{
    /// <inheritdoc/>
    public static FileId DefaultValue => From(default(uint));
}

/// <summary>
/// File ID attribute, for NexusMods API file IDs.
/// </summary>
public class FileIdAttribute(string ns, string name) : 
    ScalarAttribute<FileId, uint>(ValueTags.UInt64, ns, name)
{
    /// <inheritdoc />
    protected override uint ToLowLevel(FileId value) => value.Value;

    /// <inheritdoc />
    protected override FileId FromLowLevel(ulong value, ValueTags tags, RegistryId registryId) => FileId.From((uint)value);
}
