using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.Loadouts.ApplySteps;

/// <summary>
/// Copies a file [location known by hash, usually from archive], to a specified
/// location.
/// </summary>
/// <remarks>
///    Used when a file does not need backing up.
/// </remarks>
public record CopyFile : IApplyStep, IStaticFileStep
{
    /// <summary>
    /// The location where the file will be copied to.
    /// </summary>
    public required AbsolutePath To { get; init; }

    /// <summary>
    /// The source to copy files from.
    /// [Source is determined by hash]
    /// </summary>
    public required AStaticModFile From { get; init; }

    /// <inheritdoc />
    public Hash Hash => From.Hash;

    /// <inheritdoc />
    public Size Size => From.Size;
}
