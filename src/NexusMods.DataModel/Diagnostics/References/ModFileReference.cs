using JetBrains.Annotations;
using NexusMods.DataModel.Loadouts;

namespace NexusMods.DataModel.Diagnostics.References;

/// <summary>
/// A reference to a <see cref="AModFile"/>
/// </summary>
[PublicAPI]
public record ModFileReference : IDataReference<ModFileId, AModFile>
{
    /// <inheritdoc/>
    public required ModFileId DataId { get; init; }
}
