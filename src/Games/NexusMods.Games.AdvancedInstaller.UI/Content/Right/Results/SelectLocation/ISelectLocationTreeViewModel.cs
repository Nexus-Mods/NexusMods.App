using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation.SelectableDirectoryEntry;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation;

public interface ISelectLocationTreeViewModel : IViewModel
{
    public ITreeEntryViewModel Root { get; }

    public HierarchicalTreeDataGridSource<ITreeEntryViewModel> Tree { get; }

    /// <summary>
    ///     Coordinator for the AdvancedInstaller components.
    /// </summary>
    ISelectableDirectoryUpdateReceiver Receiver { get; }
}
