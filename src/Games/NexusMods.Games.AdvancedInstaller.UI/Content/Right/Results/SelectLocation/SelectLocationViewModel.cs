using System.Collections.ObjectModel;
using NexusMods.App.UI;
using NexusMods.App.UI.Extensions;
using NexusMods.DataModel.Games;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation;

internal class SelectLocationViewModel : AViewModel<ISelectLocationViewModel>,
    ISelectLocationViewModel
{
    public SelectLocationViewModel(GameLocationsRegister register, string gameName = "") : this() // <= remove this
    {
        List<ISelectLocationTreeViewModel> treeList = new();
        foreach (var location in register.GetTopLevelLocations())
        {
            treeList.Add(new SelectLocationTreeViewModel(register, location.Key));
        }

        AllFoldersTrees = treeList.ToReadOnlyObservableCollection();
    }

    public SelectLocationViewModel()
    {
        SuggestedEntries = Array.Empty<ISuggestedEntryViewModel>().ToReadOnlyObservableCollection();
        AllFoldersTrees = new ISelectLocationTreeViewModel[]
        {
            new SelectLocationTreeDesignViewModel(),
            new SelectLocationTreeDesignViewModel()
        }.ToReadOnlyObservableCollection();
    }

    public ReadOnlyObservableCollection<ISuggestedEntryViewModel> SuggestedEntries { get; set; }
    public ReadOnlyObservableCollection<ISelectLocationTreeViewModel> AllFoldersTrees { get; }
}
