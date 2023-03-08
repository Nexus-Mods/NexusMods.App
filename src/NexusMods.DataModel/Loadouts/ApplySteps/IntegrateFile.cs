using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.Loadouts.ApplySteps;

// TODO: Ask Tim about this.
/// <summary/>
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
