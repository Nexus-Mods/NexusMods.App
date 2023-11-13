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
}
