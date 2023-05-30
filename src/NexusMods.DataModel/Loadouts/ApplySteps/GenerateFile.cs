using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.Loadouts.ApplySteps;

public class GenerateFile : IApplyStep
{
    /// <summary>
    /// Install location of the file to be generated.
    /// </summary>
    public required AbsolutePath To { get; init; }
    
    /// <summary>
    /// The source file to be generated.
    /// </summary>
    public required IGeneratedFile Source { get; init; }
    
    /// <summary>
    /// The fingerprint created by the generator
    /// </summary>
    public required Hash Fingerprint { get; init; }
}
