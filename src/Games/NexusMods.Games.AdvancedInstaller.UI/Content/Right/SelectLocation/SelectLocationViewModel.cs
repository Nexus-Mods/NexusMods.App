using System.Collections.ObjectModel;
using NexusMods.App.UI.Extensions;
using NexusMods.DataModel.Games;
using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI.SelectLocation;

internal class SelectLocationViewModel : AViewModel<ISelectLocationViewModel>,
    ISelectLocationViewModel
{
    public ReadOnlyObservableCollection<ISuggestedEntryViewModel> SuggestedEntries { get; }
    public ReadOnlyObservableCollection<ISelectLocationTreeViewModel> AllFoldersTrees { get; }

    public SelectLocationViewModel(GameLocationsRegister register,
        IAdvancedInstallerCoordinator directorySelectedObserver,
        string gameName = "")
    {
        List<ISelectLocationTreeViewModel> treeList = new();
        ObservableCollection<ISuggestedEntryViewModel> suggestedEntries = new();

        // We add the 'game name' if we show the game folder, otherwise we use name of LocationId.
        foreach (var location in register.GetTopLevelLocations())
        {
            var treeVM = new SelectLocationTreeViewModel(location.Value, location.Key,
                location.Key == LocationId.Game ? gameName : null, directorySelectedObserver);
            treeList.Add(treeVM);

            // Add top level locations to suggested entries.
            // TODO: Add nested locations to suggested entries.
            // Warning! Each Suggested entry needs to be mapped to a tree item, which will require adding nested locations to the tree.
            suggestedEntries.Add(new SuggestedEntryViewModel(register, location.Key, null, directorySelectedObserver, treeVM.Root));
        }

        SuggestedEntries = suggestedEntries.ToReadOnlyObservableCollection();
        AllFoldersTrees = treeList.ToReadOnlyObservableCollection();
    }
}
