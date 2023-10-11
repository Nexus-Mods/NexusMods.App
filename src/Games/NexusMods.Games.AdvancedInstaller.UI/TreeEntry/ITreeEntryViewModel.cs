using NexusMods.App.UI;
using NexusMods.Games.AdvancedInstaller.UI.Content.Left;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView.PreviewEntry;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation.SelectableDirectoryEntry;
using ReactiveUI.Fody.Helpers;
using OneOf;

namespace NexusMods.Games.AdvancedInstaller.UI;

public interface ITreeEntryViewModel : IViewModel
{
    public OneOf<IModContentNode, ISelectableDirectoryNode, IPreviewEntryNode> Node { get; }

}

