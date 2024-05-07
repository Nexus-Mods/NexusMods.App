using NexusMods.MnemonicDB.Abstractions;
using TransparentValueObjects;
using File = NexusMods.Abstractions.Loadouts.Files.File;

namespace NexusMods.Abstractions.Loadouts.Ids;

/// <summary>
/// Files are the individual components of a loadout, they can be sorted by various critiera, and
/// then one winning file is written to disk when the loadout is applied.
/// </summary>
[ValueObject<EntityId>]
public readonly partial struct FileId : ITypedId<File.Model>
{
    EntityId ITypedEntityId.Value => Value;
}
