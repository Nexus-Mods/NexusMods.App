using System.Collections.ObjectModel;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.App.UI.Controls.Trees.Files;

namespace NexusMods.App.UI.Controls.ModInfo.ModFiles;

public interface IModFilesViewModel : IViewModelInterface
{
    /// <summary>
    ///     All items to be displayed.
    /// </summary>
    ReadOnlyObservableCollection<IFileTreeNodeViewModel> Items { get; }

    void Initialize(LoadoutId loadoutId, ModId modId) { }
}
