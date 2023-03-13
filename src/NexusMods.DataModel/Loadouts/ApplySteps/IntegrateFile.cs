using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.Loadouts.ApplySteps;

/// <summary>
/// Used for marking files as being 'for integration'.
///
/// i.e. When you run a game or tool, and it produces a file, such as
/// a log, or savegame; we might want to integrate those files as mods
/// in order to allow the user to manage these files.
/// </summary>
public record IntegrateFile : IApplyStep, IStaticFileStep
{
    /// <summary>
    /// Absolute path of the file to integrate.
    /// </summary>
    public required AbsolutePath To { get; init; }

    /// <summary>
    /// The mod to which this file belongs to.
    /// </summary>
    public required Mod Mod { get; init; }

    /// <inheritdoc />
    public required Size Size { get; init; }

    /// <inheritdoc />
    public required Hash Hash { get; init; }
}
