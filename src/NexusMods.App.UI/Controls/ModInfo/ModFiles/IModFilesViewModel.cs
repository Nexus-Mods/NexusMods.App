using System.Collections.ObjectModel;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.App.UI.Controls.Trees.Files;

namespace NexusMods.App.UI.Controls.ModInfo.ModFiles;
using ModFileNode = TreeNodeVM<IFileTreeNodeViewModel, GamePath>;

public interface IModFilesViewModel : IViewModelInterface
{
    /// <summary>
    ///     Location of the primary root (e.g. Game folder, Saves folder)
    /// </summary>
    string? PrimaryRootLocation { get; }
    
    /// <summary>
    ///     The number of roots present in <see cref="Items"/>
    /// </summary>
    int RootCount { get; }
    
    /// <summary>
    ///     All items to be displayed.
    /// </summary>
    ReadOnlyObservableCollection<ModFileNode> Items { get; }

    void Initialize(LoadoutId loadoutId, ModId modId) { }
}
