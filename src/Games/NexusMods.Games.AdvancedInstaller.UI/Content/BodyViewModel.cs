using System.Reactive.Disposables;
using DynamicData;
using DynamicData.Kernel;
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

using PreviewTreeNode = TreeNodeVM<IPreviewTreeEntryViewModel, GamePath>;
using ModContentTreeNode = TreeNodeVM<IModContentTreeEntryViewModel, RelativePath>;
using SelectableTreeNode = TreeNodeVM<ISelectableTreeEntryViewModel, GamePath>;

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

            // Handle starting to create a new folder
            SelectLocationViewModel.TreeEntriesCache.Connect()
                .MergeManyItems(entry => entry.EditCreateFolderCommand)
                .Subscribe(entry => OnEditCreateFolder(entry.Item))
                .DisposeWith(disposables);

            // Handle Canceling the creation of a new folder
            SelectLocationViewModel.TreeEntriesCache.Connect()
                .MergeManyItems(entry => entry.CancelCreateFolderCommand)
                .Subscribe(entry => OnCancelCreateFolder(entry.Item))
                .DisposeWith(disposables);

            // Handle Saving the creation of a new folder
            SelectLocationViewModel.TreeEntriesCache.Connect()
                .MergeManyItems(entry => entry.SaveCreatedFolderCommand)
                .Subscribe(entry => OnSaveCreateFolder(entry.Item))
                .DisposeWith(disposables);

            // Handle Deleting the creation of a new folder
            SelectLocationViewModel.TreeEntriesCache.Connect()
                .MergeManyItems(entry => entry.DeleteCreatedFolderCommand)
                .Subscribe(entry => OnDeleteCreatedFolder(entry.Item))
                .DisposeWith(disposables);

            // Handle CreateMappingCommand from SelectTreeEntry
            SelectLocationViewModel.TreeEntriesCache.Connect()
                .MergeManyItems(entry => entry.CreateMappingCommand)
                .Subscribe(entry => OnCreateMapping(entry.Item))
                .DisposeWith(disposables);
        });
    }

    #region ModContentFunctionality

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
                if (parent.Children.Any(x =>
                        x.Item.Status == ModContentTreeEntryStatus.SelectingViaParent))
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

    #endregion ModContentFunctionality

    private void OnCreateMapping(ISelectableTreeEntryViewModel selectableTreeEntryViewModel)
    {
        var targetLocation = SelectLocationViewModel.TreeEntriesCache.Lookup(selectableTreeEntryViewModel.GamePath)
            .ValueOrDefault();
        if (targetLocation is null)
            return;

        PreviewViewModel.TreeEntriesCache.Edit(previewTreeUpdater =>
        {
            // Create the stump path up to the Selected target folder.
            var mappingParentPreviewNode = PreparePreviewTargetPath(targetLocation.GamePath, previewTreeUpdater);

            foreach (var selectedModEntry in ModContentViewModel.SelectedEntriesCache.Items)
            {
                if (selectedModEntry.IsRoot)
                {
                    selectedModEntry.MappingFolderName = targetLocation.DisplayName;
                    selectedModEntry.Status = ModContentTreeEntryStatus.IncludedExplicit;
                    // Don't create mapping for the root element, just map the children to the target folder.
                    MapChildrenRecursive(ModContentViewModel.Root, mappingParentPreviewNode.Item, previewTreeUpdater);
                    continue;
                }

                var entryMappingPath = new GamePath(targetLocation.GamePath.LocationId,
                    targetLocation.GamePath.Path.Join(selectedModEntry.RelativePath.FileName));
                var previewEntry = previewTreeUpdater.Lookup(entryMappingPath).ValueOrDefault();

                if (selectedModEntry.IsDirectory)
                {
                    var selectedModNode = ModContentViewModel.Root.FindNode(selectedModEntry.RelativePath);
                    if (selectedModNode is null)
                        continue;

                    if (previewEntry is null)
                    {
                        previewEntry = new PreviewTreeEntryViewModel(entryMappingPath, true, true);
                        previewTreeUpdater.AddOrUpdate(previewEntry);
                    } else
                    {
                        previewEntry.IsFolderMerged = true;
                        previewEntry.IsRemovable = true;
                    }

                    CreateDirectoryMapping(selectedModNode, previewEntry, previewTreeUpdater, true);
                    continue;
                }

                // File mapping
                if (previewEntry is null)
                {
                    previewEntry = new PreviewTreeEntryViewModel(entryMappingPath, false, true);
                    previewTreeUpdater.AddOrUpdate(previewEntry);
                }
                else
                {
                    RemoveFileMapping(previewEntry);
                }
                CreateFileMapping(selectedModEntry, previewEntry, true);
            }
        });

        CurrentRightContentViewModel = PreviewViewModel;
    }

    private void CreateDirectoryMapping(ModContentTreeNode sourceNode,
        IPreviewTreeEntryViewModel destPreviewEntry,
        ISourceUpdater<IPreviewTreeEntryViewModel, GamePath> previewTreeUpdater, bool isExplicit = false)
    {
        sourceNode.Item.Mapping = destPreviewEntry.GamePath;
        sourceNode.Item.MappingFolderName = PreviewViewModel.TreeEntriesCache.Lookup(destPreviewEntry.Parent)
            .ValueOrDefault()?.DisplayName ?? string.Empty;

        MapChildrenRecursive(sourceNode, destPreviewEntry, previewTreeUpdater);

        sourceNode.Item.Status = isExplicit
            ? ModContentTreeEntryStatus.IncludedExplicit
            : ModContentTreeEntryStatus.IncludedViaParent;
    }

    private void CreateFileMapping(IModContentTreeEntryViewModel sourceEntry,
        IPreviewTreeEntryViewModel destPreviewEntry, bool isExplicit)
    {
        destPreviewEntry.AddFileMapping(sourceEntry);
        var mappingFolderName = PreviewViewModel.TreeEntriesCache.Lookup(destPreviewEntry.Parent)
            .ValueOrDefault()?.DisplayName;
        sourceEntry.AddFileMapping(destPreviewEntry, mappingFolderName ?? string.Empty, isExplicit);
        DeploymentData.AddMapping(sourceEntry.RelativePath, destPreviewEntry.GamePath);
    }

    private void RemoveFileMapping(IPreviewTreeEntryViewModel previewEntry)
    {
        if (previewEntry.MappedEntry is null)
            return;
        var modEntry = previewEntry.MappedEntry;

        previewEntry.RemoveFileMapping();
        modEntry.RemoveFileMapping();
        DeploymentData.RemoveMapping(modEntry.RelativePath);
    }

    private void CreateMapping(ModContentTreeNode sourceNode,
        GamePath parentDestinationPath)
    {
        // Create matching preview node for this mapping
        var targetPath = sourceNode.Item.RelativePath == RelativePath.Empty
            ? parentDestinationPath.Path
            : parentDestinationPath.Path.Join(parentDestinationPath.FileName);
    }

    /// <summary>
    /// This creates the stump path up to the Selected target folder.
    /// All the created nodes are directories and they don't have a mapping, so they are not New.
    /// </summary>
    /// <param name="targetFolder">The GamePath to the folder selected as install location</param>
    /// <param name="previewTreeUpdater">SourceCache updater to make all changes in one operation.</param>
    /// <returns></returns>
    private PreviewTreeNode PreparePreviewTargetPath(GamePath targetFolder,
        ISourceUpdater<IPreviewTreeEntryViewModel, GamePath> previewTreeUpdater)
    {
        List<IPreviewTreeEntryViewModel> treeEntries = new();

        foreach (var subPath in targetFolder.GetAllParents().Reverse())
        {
            var existingEntry = previewTreeUpdater.Lookup(subPath).ValueOrDefault();
            if (existingEntry != null)
            {
                continue;
            }

            var newEntry = new PreviewTreeEntryViewModel(
                subPath,
                true,
                false);

            treeEntries.Add(newEntry);
        }

        if (treeEntries.Count > 0)
        {
            previewTreeUpdater.AddOrUpdate(treeEntries);
        }

        // We just added the node, so this should never be null.
        return PreviewViewModel.TreeRoots.FirstOrDefault(node => node.Id.LocationId == targetFolder.LocationId)
            ?.FindNode(targetFolder)!;
    }

    private void MapChildrenRecursive(ModContentTreeNode sourceNode,
        IPreviewTreeEntryViewModel mappingParentPreviewNode,
        ISourceUpdater<IPreviewTreeEntryViewModel, GamePath> previewTreeUpdater)
    {
        foreach (var child in sourceNode.Children)
        {
            if (child.Item.Status != ModContentTreeEntryStatus.SelectingViaParent) continue;

            var entryMappingPath = new GamePath(mappingParentPreviewNode.GamePath.LocationId,
                mappingParentPreviewNode.GamePath.Path.Join(child.Item.RelativePath.FileName));
            var previewEntry = previewTreeUpdater.Lookup(entryMappingPath).ValueOrDefault();

            if (child.Item.IsDirectory)
            {
                if (previewEntry is null)
                {
                    previewEntry = new PreviewTreeEntryViewModel(entryMappingPath, true, true);
                    previewTreeUpdater.AddOrUpdate(previewEntry);
                }
                else
                {
                    previewEntry.IsFolderMerged = true;
                    previewEntry.IsRemovable = true;
                }

                CreateDirectoryMapping(child, previewEntry, previewTreeUpdater);
                continue;
            }

            // File mapping
            if (previewEntry is null)
            {
                previewEntry = new PreviewTreeEntryViewModel(entryMappingPath, false, true);
                previewTreeUpdater.AddOrUpdate(previewEntry);
            }
            else
            {
                RemoveFileMapping(previewEntry);
            }
            CreateFileMapping(child.Item, previewEntry, false);
        }
    }

    #region CreateFolderFunctionality

    private void OnEditCreateFolder(ISelectableTreeEntryViewModel selectableTreeEntryViewModel)
    {
        var foundNode = SelectLocationViewModel.TreeRoots
            .FirstOrDefault(root => root.Item.GamePath.LocationId == selectableTreeEntryViewModel.GamePath.LocationId)
            ?.FindNode(selectableTreeEntryViewModel.GamePath);
        if (foundNode is null)
            return;

        foundNode.Item.InputText = string.Empty;

        // Set the status of the parent node to Edit
        foundNode.Item.Status = SelectableDirectoryNodeStatus.Editing;

        // TODO: Disable all other buttons while editing
    }

    private void OnCancelCreateFolder(ISelectableTreeEntryViewModel selectableTreeEntryViewModel)
    {
        var foundNode = SelectLocationViewModel.TreeRoots
            .FirstOrDefault(root => root.Item.GamePath.LocationId == selectableTreeEntryViewModel.GamePath.LocationId)
            ?.FindNode(selectableTreeEntryViewModel.GamePath);
        if (foundNode is null)
            return;

        // Reset the status to Create
        foundNode.Item.Status = SelectableDirectoryNodeStatus.Create;
        foundNode.Item.InputText = string.Empty;
    }

    private void OnSaveCreateFolder(ISelectableTreeEntryViewModel selectableTreeEntryViewModel)
    {
        var foundNode = SelectLocationViewModel.TreeRoots
            .FirstOrDefault(root => root.Item.GamePath.LocationId == selectableTreeEntryViewModel.GamePath.LocationId)
            ?.FindNode(selectableTreeEntryViewModel.GamePath);
        if (foundNode is null)
            return;

        var folderName = foundNode.Item.GetSanitizedInput();
        if (folderName == RelativePath.Empty)
            return;

        // Create a new child node in the parent with the given name.
        var newNode = new SelectableTreeEntryViewModel(
            new GamePath(foundNode.Item.GamePath.LocationId, foundNode.Item.GamePath.Parent.Path.Join(folderName)),
            SelectableDirectoryNodeStatus.Created);

        // Add a new CreateFolder node under it.
        var newNestedCreateFolder = new SelectableTreeEntryViewModel(
            new GamePath(newNode.GamePath.LocationId, newNode.GamePath.Path.Join("*CreateFolder*")),
            SelectableDirectoryNodeStatus.Create);

        // Add the nodes to the tree cache
        SelectLocationViewModel.TreeEntriesCache.AddOrUpdate(new[] { newNode, newNestedCreateFolder });

        // Reset this to Create state.
        foundNode.Item.Status = SelectableDirectoryNodeStatus.Create;
        foundNode.Item.InputText = string.Empty;
    }

    private void OnDeleteCreatedFolder(ISelectableTreeEntryViewModel selectableTreeEntryViewModel)
    {
        var foundNode = SelectLocationViewModel.TreeRoots
            .FirstOrDefault(root => root.Item.GamePath.LocationId == selectableTreeEntryViewModel.GamePath.LocationId)
            ?.FindNode(selectableTreeEntryViewModel.GamePath);
        if (foundNode is null) return;

        // Remove the node and all children from the cache
        var idsToRemove = foundNode.GetAllDescendentIds().Append(foundNode.Item.GamePath);
        SelectLocationViewModel.TreeEntriesCache.Remove(idsToRemove);
    }

    #endregion CreateFolderFunctionality
}
