using System.Diagnostics.CodeAnalysis;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.Abstractions.MnemonicDB.Attributes.Extensions;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Loadouts.Files;

/// <summary>
/// Deleted files in the app are reified, that is to say they exist as a item in the datamodel. This
/// is to allow for tracking of deletes on disk without having to modify mods or files directly. So Mod
/// A may provide a file A, and then Mod B may also provide file A, and then in the `Overrides` A may also
/// exist, but marked as deleted. This means the mods can remain unmodified, and the overrides can be
/// the final say on what is installed.
/// </summary>
[Include<File>]
[Obsolete(message: "This will be replaced with `DeletedFile` (LoadoutItem)")]
public partial class DeletedFile : IModelDefinition
{
    private const string Namespace = "NexusMods.Abstractions.Loadouts.Files.DeletedFile";

    /// <summary>
    /// Not strictly necessary, but we need some attribute on this entity so we can query
    /// it and discriminate it from other entities.
    /// </summary>
    public static readonly SizeAttribute Size = new(Namespace, nameof(Size));
}
