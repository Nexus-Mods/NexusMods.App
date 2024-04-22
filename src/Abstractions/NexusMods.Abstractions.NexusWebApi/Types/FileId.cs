using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using TransparentValueObjects;

namespace NexusMods.Abstractions.NexusWebApi.Types;

/// <summary>
/// Unique ID for a game file hosted on a mod page.
///
/// This ID is unique within the context of the game.
/// i.e. This ID might be used for another mod if you search for mods for another game.
/// </summary>
[ValueObject<ulong>]
public readonly partial struct FileId : IAugmentWith<DefaultValueAugment>
{
    /// <inheritdoc/>
    public static FileId DefaultValue => From(default);
}


/// <summary>
/// File ID attribute, for NexusMods API file IDs.
/// </summary>
public class FileIdAttribute(string ns, string name) : 
    ScalarAttribute<FileId, ulong>(ValueTags.UInt64, ns, name)
{
    /// <inheritdoc />
    protected override ulong ToLowLevel(FileId value) => value.Value;

    /// <inheritdoc />
    protected override FileId FromLowLevel(ulong value, ValueTags tags) => FileId.From(value);
}
