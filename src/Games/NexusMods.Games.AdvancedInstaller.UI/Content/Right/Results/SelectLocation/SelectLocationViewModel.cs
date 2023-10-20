using System.Collections.ObjectModel;
using Avalonia.Controls;
using NexusMods.App.UI;
using NexusMods.App.UI.Extensions;
using NexusMods.DataModel.Games;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView.PreviewEntry;
using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation;

internal class SelectLocationViewModel : AViewModel<ISelectLocationViewModel>,
    ISelectLocationViewModel
{
    public SelectLocationViewModel(GameLocationsRegister register, string gameName = "") : this() // <= remove this
    {
        // TODO: Implement this when the UI side (AL) is done adjusting this.
        foreach (var location in register.GetTopLevelLocations())
        {

        }
    }

    public SelectLocationViewModel()
    {
        SuggestedEntries = Array.Empty<ISuggestedEntryViewModel>().ToReadOnlyObservableCollection();
        TreeRoot = new TreeEntryViewModel(PreviewEntryNode.Create(new GamePath(LocationId.Game, ""), true));
        Tree = new HierarchicalTreeDataGridSource<ITreeEntryViewModel>(TreeRoot);
    }

    public ReadOnlyObservableCollection<ISuggestedEntryViewModel> SuggestedEntries { get; set; }
    public HierarchicalTreeDataGridSource<ITreeEntryViewModel> Tree { get; }
    public ITreeEntryViewModel TreeRoot { get; }
}
