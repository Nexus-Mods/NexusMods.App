using NexusMods.DataModel.Games;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Games.AdvancedInstaller.UI.Content.Left;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.EmptyPreview;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation.SelectableDirectoryEntry;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;
using ReactiveUI.Fody.Helpers;
using ITreeEntryViewModel = NexusMods.Games.AdvancedInstaller.UI.Content.Left.ITreeEntryViewModel;

namespace NexusMods.Games.AdvancedInstaller.UI.Content;

internal class BodyViewModel : AViewModel<IBodyViewModel>,
    IBodyViewModel, IAdvancedInstallerCoordinator
{
    public BodyViewModel(FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles, GameLocationsRegister register,
        string gameName = "")
    {
        ModContentViewModel = new ModContentViewModel(archiveFiles, this);
        SelectLocationViewModel = new SelectLocationViewModel(register, this, gameName);
        CurrentPreviewViewModel = EmptyPreviewViewModel;
    }

    public DeploymentData Data { get; set; } = new();

    public IModContentViewModel ModContentViewModel { get; }
    public IPreviewViewModel PreviewViewModel { get; } = new PreviewViewModel();
    public IEmptyPreviewViewModel EmptyPreviewViewModel { get; } = new EmptyPreviewViewModel();
    public ISelectLocationViewModel SelectLocationViewModel { get; }
    [Reactive] public IViewModel CurrentPreviewViewModel { get; set; }

    internal readonly List<ITreeEntryViewModel> SelectedItems = new();

    #region IModContentUpdateReceiver

    public void OnSelect(ITreeEntryViewModel treeEntryViewModel)
    {
        SelectedItems.Add(treeEntryViewModel);
        CurrentPreviewViewModel = SelectLocationViewModel;
    }

    public void OnCancelSelect(ITreeEntryViewModel treeEntryViewModel)
    {
        SelectedItems.Remove(treeEntryViewModel);
        if (SelectedItems.Count == 0)
            CurrentPreviewViewModel = HasAnyItemsToPreview() ? PreviewViewModel : EmptyPreviewViewModel;
    }

    private bool HasAnyItemsToPreview()
    {
        foreach (var location in PreviewViewModel.Locations)
        {
            if (location.Root.Children.Count > 0)
                return true;
        }

        return false;
    }

    #endregion

    #region ISelectableDirectoryUpdateReceiver

    public void OnDirectorySelected(Right.Results.SelectLocation.SelectableDirectoryEntry.ITreeEntryViewModel directory)
    {
        foreach (var item in SelectedItems)
        {
            var node = PreviewViewModel.GetOrCreateBindingTarget(directory.Path);
            item.Link(Data, node, directory.Status == SelectableDirectoryNodeStatus.Created);
        }

        SelectedItems.Clear();
        CurrentPreviewViewModel = PreviewViewModel;
    }

    #endregion
}
