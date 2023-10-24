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

    public SelectLocationViewModel(GameLocationsRegister register, string gameName = "")
    {
        List<ISelectLocationTreeViewModel> treeList = new();

        // We add the 'game name' if we show the game folder, otherwise we use name of LocationId.
        foreach (var location in register.GetTopLevelLocations())
            treeList.Add(new SelectLocationTreeViewModel(location.Value, location.Key,
                location.Key == LocationId.Game ? gameName : null));

        SuggestedEntries = new(new());
        AllFoldersTrees = treeList.ToReadOnlyObservableCollection();
    }
}
