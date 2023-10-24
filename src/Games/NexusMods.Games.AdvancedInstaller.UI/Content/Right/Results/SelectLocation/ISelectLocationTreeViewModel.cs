using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation.SelectableDirectoryEntry;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation;

public interface ISelectLocationTreeViewModel : IViewModel
{
    public HierarchicalTreeDataGridSource<ITreeEntryViewModel> Tree { get; }
}
