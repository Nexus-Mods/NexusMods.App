using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Loadouts;

/// <summary>
/// Represents a deleted file.
/// </summary>
[PublicAPI]
[Include<LoadoutItemWithTargetPath>]
public partial class DeletedFile : IModelDefinition
{
    private const string Namespace = "NexusMods.Loadouts.DeletedFile";

    /// <summary>
    /// Reason.
    /// </summary>
    public static readonly StringAttribute Reason = new(Namespace, nameof(Reason));
}
