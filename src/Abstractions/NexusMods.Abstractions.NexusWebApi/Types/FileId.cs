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
    public static FileId DefaultValue => FileId.From(default);
}
