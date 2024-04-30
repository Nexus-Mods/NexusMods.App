using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.Trees;
using File = NexusMods.Abstractions.Loadouts.Files.File;

namespace NexusMods.Abstractions.Games.DTO;

/// <summary>
/// A file tree is a tree that contains all files from a loadout, flattened into a single tree.
/// </summary>
public class FlattenedLoadout : AGamePathNodeTree<File.Model>
{
    private FlattenedLoadout(IEnumerable<KeyValuePair<GamePath, File.Model>> tree) : base(tree) { }

    /// <summary>
    ///     Creates a tree that contains all files from a loadout, flattened into a single tree.
    /// </summary>
    public static FlattenedLoadout Create(IEnumerable<KeyValuePair<GamePath, File.Model>> items) => new(items);
}
