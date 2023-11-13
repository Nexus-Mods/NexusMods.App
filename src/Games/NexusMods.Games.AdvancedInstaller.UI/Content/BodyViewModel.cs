using System.Reactive.Disposables;
using DynamicData;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Games.AdvancedInstaller.UI.EmptyPreview;
using NexusMods.Games.AdvancedInstaller.UI.ModContent;
using NexusMods.Games.AdvancedInstaller.UI.Preview;
using NexusMods.Games.AdvancedInstaller.UI.SelectLocation;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.AdvancedInstaller.UI;

public class BodyViewModel : AViewModel<IBodyViewModel>, IBodyViewModel
{
    public string ModName { get; set; }
    [Reactive] public bool CanInstall { get; private set; }
    [Reactive] public IViewModelInterface CurrentRightContentViewModel { get; private set; }
    public IModContentViewModel ModContentViewModel { get; }
    public IEmptyPreviewViewModel EmptyPreviewViewModel { get; }
    public ISelectLocationViewModel SelectLocationViewModel { get; }
    public IPreviewViewModel PreviewViewModel { get; }

    public DeploymentData DeploymentData { get; }


    public BodyViewModel(
        DeploymentData data,
        string modName,
        FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles,
        GameLocationsRegister locationRegister,
        Loadout? loadout)
    {
        // Setup child VMs
        ModName = modName;
        DeploymentData = data;
        CanInstall = false;

        EmptyPreviewViewModel = new EmptyPreviewViewModel();

        ModContentViewModel = new ModContentViewModel(archiveFiles);

        SelectLocationViewModel = new SelectLocationViewModel(locationRegister, loadout);

        PreviewViewModel = new PreviewViewModel();

        CurrentRightContentViewModel = EmptyPreviewViewModel;

        // Setup functionality
        this.WhenActivated(disposables =>
        {
            // Handle user selecting mod content entries
            ModContentViewModel.ModContentEntriesCache.Connect()
                .MergeManyItems(entry => entry.BeginSelectCommand)
                .Subscribe(entry => OnBeginSelect(entry.Item))
                .DisposeWith(disposables);

            // Handle user cancelling the selection of mod content entries
            ModContentViewModel.ModContentEntriesCache.Connect()
                .MergeManyItems(entry => entry.CancelSelectCommand)
                .Subscribe(entry => OnCancelSelect(entry.Item))
                .DisposeWith(disposables);
        });
    }

    /// <summary>
    /// Recursively update the tree nodes state to be selected.
    /// Update the SelectedEntriesCache.
    /// </summary>
    /// <param name="modContentTreeEntryViewModel"></param>
    private void OnBeginSelect(IModContentTreeEntryViewModel modContentTreeEntryViewModel)
    {
        var foundNode = ModContentViewModel.Root.FindNode(modContentTreeEntryViewModel.RelativePath);
        if (foundNode is null)
            return;

        // Set the status of the parent node to Selecting
        foundNode.Item.Status = ModContentTreeEntryStatus.Selecting;

        // Set the status of all the children to SelectingViaParent
        ModContentViewModel.SelectChildrenRecursive(foundNode);
        ModContentViewModel.SelectedEntriesCache.AddOrUpdate(modContentTreeEntryViewModel);

        // Update the UI to show the SelectLocationViewModel
        CurrentRightContentViewModel = SelectLocationViewModel;
    }

    private void OnCancelSelect(IModContentTreeEntryViewModel modContentTreeEntryViewModel)
    {
        var foundNode = ModContentViewModel.Root.FindNode(modContentTreeEntryViewModel.RelativePath);
        if (foundNode is null)
            return;

        var oldStatus = foundNode.Item.Status;

        // Set the status of the parent node to Default
        foundNode.Item.Status = ModContentTreeEntryStatus.Default;

        // Set the status of all the children to Default
        ModContentViewModel.DeselectChildrenRecursive(foundNode);
        ModContentViewModel.SelectedEntriesCache.Remove(modContentTreeEntryViewModel);

        // Check if ancestors need to be cleaned up (no more selected children).
        if (oldStatus == ModContentTreeEntryStatus.SelectingViaParent)
        {
            var parent = ModContentViewModel.Root.FindNode(foundNode.Item.Parent);
            while (parent is not null)
            {
                if (parent.Children.Any(x => x.Item.Status == ModContentTreeEntryStatus.SelectingViaParent))
                    break;
                var prevStatus = parent.Item.Status;

                // If no more children are selected, reset the status of the parent node to Default
                parent.Item.Status = ModContentTreeEntryStatus.Default;

                if (prevStatus == ModContentTreeEntryStatus.Selecting)
                {
                    // If the parent node was Selecting, remove it from the SelectedEntriesCache and stop
                    ModContentViewModel.SelectedEntriesCache.Remove(parent.Item);
                    break;
                }

                // This was a SelectingViaParent node as well, check the next parent
                parent = ModContentViewModel.Root.FindNode(parent.Item.Parent);
            }
        }

        // If no more items are selected, show either the preview or empty preview
        CurrentRightContentViewModel = ModContentViewModel.SelectedEntriesCache.Count > 0
            ? SelectLocationViewModel
            : DeploymentData.ArchiveToOutputMap.Count > 0
                ? PreviewViewModel
                : EmptyPreviewViewModel;
    }
}
