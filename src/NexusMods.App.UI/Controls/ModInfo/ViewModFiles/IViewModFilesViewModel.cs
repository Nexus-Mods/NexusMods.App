using System.Collections.ObjectModel;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.App.UI.Controls.Trees.Files;

namespace NexusMods.App.UI.Controls.ModInfo.ViewModFiles;
using ModFileNode = TreeNodeVM<IFileTreeNodeViewModel, Abstractions.GameLocators.GamePath>;

public interface IViewModFilesViewModel : IViewModelInterface
{
    ReadOnlyObservableCollection<ModFileNode> Items { get; }
    
    void Initialize(LoadoutId loadoutId, List<ModId> contextModIds);
}
