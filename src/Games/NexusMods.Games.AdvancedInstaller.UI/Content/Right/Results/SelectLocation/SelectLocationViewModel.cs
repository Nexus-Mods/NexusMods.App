using System.Collections.ObjectModel;
using Avalonia.Controls;
using NexusMods.App.UI;
using NexusMods.App.UI.Extensions;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView.PreviewEntry;
using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation;

internal class SelectLocationViewModel : AViewModel<ISelectLocationViewModel>,
    ISelectLocationViewModel
{
    public SelectLocationViewModel() : base()
    {
        SuggestedEntries = Array.Empty<ISuggestedEntryViewModel>().ToReadOnlyObservableCollection();

        // TODO: Pass the ISelectableDirectoryNode tree instead
        Tree = new HierarchicalTreeDataGridSource<ITreeEntryViewModel>(
            new TreeEntryViewModel(PreviewEntryNode.Create(new GamePath(LocationId.Game, ""), true)));
    }

    public ReadOnlyObservableCollection<ISuggestedEntryViewModel> SuggestedEntries { get; set; }
    public HierarchicalTreeDataGridSource<ITreeEntryViewModel> Tree { get; }
}
