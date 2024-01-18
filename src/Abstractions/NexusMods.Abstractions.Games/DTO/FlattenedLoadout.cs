using NexusMods.Abstractions.Games.Legacy;
using NexusMods.Abstractions.Installers.DTO;

namespace NexusMods.Abstractions.Games.DTO;

/// <summary>
/// A flattened loadout is a tree that contains all files from a loadout, flattened into a single tree.
/// </summary>
public class FlattenedLoadout : AGamePathTree<ModFilePair>
{
    private FlattenedLoadout(IEnumerable<KeyValuePair<GamePath, ModFilePair>> tree) : base(tree)
    {

    }

    /// <summary>
    /// Creates a flattened loadout from a list mod files.
    /// </summary>
    /// <param name="tree"></param>
    /// <returns></returns>
    public static FlattenedLoadout Create(IEnumerable<KeyValuePair<GamePath, ModFilePair>> tree)
    {
        return new FlattenedLoadout(tree);
    }
}
