using System.Collections.ObjectModel;
using NexusMods.App.UI.Extensions;
using NexusMods.DataModel.Games;
using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation;

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
            treeList.Add(new SelectLocationTreeViewModel(location.Value, location.Key,
                location.Key == LocationId.Game ? gameName : null, directorySelectedObserver));

            // Add suggested entries, include nested locations like Game/Data as well.
            suggestedEntries.Add(new SuggestedEntryViewModel(register, location.Key, null, directorySelectedObserver));
            foreach (var nestedLocation in register.GetNestedLocations(location.Key))
            {
                suggestedEntries.Add(new SuggestedEntryViewModel(register, nestedLocation, null, directorySelectedObserver));
            }
        }

        SuggestedEntries = suggestedEntries.ToReadOnlyObservableCollection();
        AllFoldersTrees = treeList.ToReadOnlyObservableCollection();
    }
}
