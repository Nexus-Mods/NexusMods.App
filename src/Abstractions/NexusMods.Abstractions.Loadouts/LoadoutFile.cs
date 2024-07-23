using JetBrains.Annotations;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Loadouts;

/// <summary>
/// Represents a file in a Loadout.
/// </summary>
[Include<LoadoutItemWithTargetPath>]
[PublicAPI]
public partial class LoadoutFile : IModelDefinition
{
    private const string Namespace = "NexusMods.Loadouts.LoadoutFile";

    /// <summary>
    /// Hash of the file.
    /// </summary>
    public static readonly HashAttribute Hash = new(Namespace, nameof(Hash)) { IsIndexed = true };

    /// <summary>
    /// Size of the file.
    /// </summary>
    public static readonly SizeAttribute Size = new(Namespace, nameof(Size));
}
