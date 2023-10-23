using NexusMods.Games.AdvancedInstaller.UI.Content.Left;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation.SelectableDirectoryEntry;
using OneOf;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView.PreviewEntry;

public interface IPreviewTreeEntryViewModel : IViewModel
{
    public OneOf<ITreeEntryViewModel, ISelectableDirectoryNode, IPreviewEntryNode> Node { get; }
}
