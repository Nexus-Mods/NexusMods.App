using NexusMods.DataModel.Games;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.Paths;

namespace NexusMods.DataModel.TransformerHooks;

/// <summary>
/// A hook that is only executed on files that match the specified
/// game domains and file paths.
/// </summary>
public interface IFileFilteringHook : IGameFilteringHook
{
    /// <summary>
    /// Only execute this hook the given files
    /// </summary>
    public IEnumerable<GamePath> Files { get; }
}
