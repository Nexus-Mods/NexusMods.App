using JetBrains.Annotations;

namespace NexusMods.Abstractions.NexusModsLibrary.Models;

/// <summary>
/// Status of a revision.
/// </summary>
[PublicAPI]
public enum RevisionStatus
{
    /// <summary>
    /// The revision is a draft and can be edited.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// The revision is published and can't be edited.
    /// </summary>
    Published = 1,
}
