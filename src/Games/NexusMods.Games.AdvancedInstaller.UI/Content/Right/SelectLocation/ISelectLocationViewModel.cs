using System.Collections.ObjectModel;
using DynamicData;
using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI.SelectLocation;

public interface ISelectLocationViewModel : IViewModelInterface
{
    public ReadOnlyObservableCollection<ISuggestedEntryViewModel> SuggestedEntries { get; }

    public ReadOnlyObservableCollection<ILocationTreeContainerViewModel> TreeContainers { get; }

    public SourceCache<ISelectableTreeEntryViewModel, GamePath> TreeEntriesCache { get; }

    public ReadOnlyObservableCollection<TreeNodeVM<ISelectableTreeEntryViewModel, GamePath>> TreeRoots { get; }
}
