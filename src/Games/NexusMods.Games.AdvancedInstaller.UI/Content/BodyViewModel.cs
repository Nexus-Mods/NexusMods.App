using System.Reactive;
using System.Reactive.Disposables;
using DynamicData;
using DynamicData.Binding;
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

            // Handle CreateMappingCommand from PreviewTreeEntry
            SelectLocationViewModel.SuggestedEntries.ToObservableChangeSet(entry => entry.Id)
                .MergeManyItems(entry => entry.CreateMappingCommand)
                .Subscribe(entry => OnCreateMappingFromSuggestedEntry(entry.Item))
                .DisposeWith(disposables);

            // Handle RemoveMappingCommand from PreviewTreeEntry
            PreviewViewModel.TreeEntriesCache.Connect()
                .MergeManyItems(entry => entry.RemoveMappingCommand)
                .Subscribe(entry => OnRemoveMappingFromPreview(entry.Item))
                .DisposeWith(disposables);

            // Handle RemoveMappingCommand from ModContentTreeEntry
            ModContentViewModel.ModContentEntriesCache.Connect()
                .MergeManyItems(entry => entry.RemoveMappingCommand)
                .Subscribe(entry => OnRemoveMappingFromModContent(entry.Item))
                .DisposeWith(disposables);

            // Update CanInstall when the PreviewViewModel changes
            PreviewViewModel.TreeRoots.WhenAnyValue(roots => roots.Count)
                .Subscribe(count => { CanInstall = count > 0; })
                .DisposeWith(disposables);
        });
    }

    #region ModContentSelectionFunctionality

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

    #endregion ModContentSelectionFunctionality

    #region CreateMappingFunctionality

    private void OnCreateMappingFromSuggestedEntry(ISuggestedEntryViewModel suggestedEntry)
    {
        // Find the corresponding ISelectableTreeEntryViewModel entry
        var correspondingTreeEntry = SelectLocationViewModel.TreeEntriesCache
            .Lookup(suggestedEntry.RelativeToTopLevelLocation).ValueOrDefault();
        if (correspondingTreeEntry is not null)
            OnCreateMapping(correspondingTreeEntry);
    }

    private void OnCreateMapping(ISelectableTreeEntryViewModel selectableTreeEntryViewModel)
    {
        var targetLocation = SelectLocationViewModel.TreeEntriesCache.Lookup(selectableTreeEntryViewModel.GamePath)
            .ValueOrDefault();
        if (targetLocation is null)
            return;

        PreviewViewModel.TreeEntriesCache.Edit(previewTreeUpdater =>
        {
            // Ensure the path up to the target folder exists in the preview tree.
            var mappingParentPreviewEntry = PreparePreviewTargetPath(targetLocation.GamePath, previewTreeUpdater);

            foreach (var selectedModEntry in ModContentViewModel.SelectedEntriesCache.Items)
            {
                if (selectedModEntry.IsRoot)
                {
                    // Map the root node directly to the target folder, without creating a corresponding child node
                    selectedModEntry.SetFileMapping(mappingParentPreviewEntry, mappingParentPreviewEntry.DisplayName, true);
                    mappingParentPreviewEntry.MappedEntries.Add(selectedModEntry);

                    MapChildrenRecursive(ModContentViewModel.Root, mappingParentPreviewEntry, previewTreeUpdater);
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
                    }
                    else
                    {
                        previewEntry.IsFolderMerged = true;
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
                    RemovePreviousFileMapping(previewEntry);
                }

                CreateFileMapping(selectedModEntry, previewEntry, true);
            }
        });
        ModContentViewModel.SelectedEntriesCache.Clear();
        CurrentRightContentViewModel = PreviewViewModel;
    }

    private void CreateDirectoryMapping(ModContentTreeNode sourceNode,
        IPreviewTreeEntryViewModel destPreviewEntry,
        ISourceUpdater<IPreviewTreeEntryViewModel, GamePath> previewTreeUpdater, bool isExplicit = false)
    {
        destPreviewEntry.AddMapping(sourceNode.Item);

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
        destPreviewEntry.AddMapping(sourceEntry);
        var mappingFolderName = PreviewViewModel.TreeEntriesCache.Lookup(destPreviewEntry.Parent)
            .ValueOrDefault()?.DisplayName;
        sourceEntry.SetFileMapping(destPreviewEntry, mappingFolderName ?? string.Empty, isExplicit);
        DeploymentData.AddMapping(sourceEntry.RelativePath, destPreviewEntry.GamePath);
    }

    private void RemovePreviousFileMapping(IPreviewTreeEntryViewModel previewEntry)
    {
        if (previewEntry.MappedEntry is null)
            return;
        var modEntry = previewEntry.MappedEntry;

        previewEntry.RemoveFileMapping();
        modEntry.RemoveMapping();
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
    private IPreviewTreeEntryViewModel PreparePreviewTargetPath(GamePath targetFolder,
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

        return previewTreeUpdater.Lookup(targetFolder).ValueOrDefault()!;
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
                RemovePreviousFileMapping(previewEntry);
            }

            CreateFileMapping(child.Item, previewEntry, false);
        }
    }

    #endregion CreateMappingFunctionality

    #region RemoveMappingFunctionality

    private void OnRemoveMappingFromPreview(IPreviewTreeEntryViewModel previewEntry)
    {
        StartRemoveMappingFromPreview(previewEntry);
        CleanupPreviewTree(previewEntry.GamePath);

        // Switch to empty view if nothing is mapped anymore
        CurrentRightContentViewModel = PreviewViewModel.TreeEntriesCache.Count > 0
            ? PreviewViewModel
            : EmptyPreviewViewModel;
    }

    private void StartRemoveMappingFromPreview(IPreviewTreeEntryViewModel previewEntry)
    {
        if (previewEntry.IsDirectory)
        {
            RemoveDirectoryMappingFromPreviewRecursive(previewEntry);
        }
        else
        {
            RemoveFileMappingFromPreview(previewEntry);
        }
    }

    private void RemoveDirectoryMappingFromPreviewRecursive(IPreviewTreeEntryViewModel previewEntry)
    {
        if (!previewEntry.IsDirectory)
            return;

        foreach (var mappedEntry in previewEntry.MappedEntries.ToArray())
        {
            // Will also remove this preview node.
            StartRemoveMapping(mappedEntry);
        }

        // Check if the node still exists and has any children left
        var previewNode = PreviewViewModel.TreeRoots
            .FirstOrDefault(root => root.Item.GamePath.LocationId == previewEntry.GamePath.LocationId)?
            .FindNode(previewEntry.GamePath);

        if (previewNode is not null && previewNode.Children.Count != 0)
        {
            // User removed the item from preview, we need force remove all children
            foreach (var child in previewNode.Children.ToArray())
            {
                StartRemoveMappingFromPreview(child.Item);
            }
        }

        // Now that all descendent have been properly unmapped, we can remove this node.
        PreviewViewModel.TreeEntriesCache.Remove(previewEntry);
    }

    private void RemoveFileMappingFromPreview(IPreviewTreeEntryViewModel previewEntry)
    {
        if (previewEntry.MappedEntry is null)
        {
            previewEntry.RemoveFileMapping();
            PreviewViewModel.TreeEntriesCache.Remove(previewEntry);
            return;
        }
        // Will also remove this preview node.
        StartRemoveMapping(previewEntry.MappedEntry!);
    }

    private void OnRemoveMappingFromModContent(IModContentTreeEntryViewModel modEntry)
    {
        var mappingPath = modEntry.Mapping ?? new GamePath(LocationId.Unknown, RelativePath.Empty);

        StartRemoveMapping(modEntry);
        CleanupPreviewTree(mappingPath);

        // Switch to empty view if nothing is mapped anymore
        CurrentRightContentViewModel = PreviewViewModel.TreeEntriesCache.Count > 0
            ? PreviewViewModel
            : EmptyPreviewViewModel;
    }

    private void StartRemoveMapping(IModContentTreeEntryViewModel modEntry)
    {
        var wasMappedViaParent = modEntry.Status == ModContentTreeEntryStatus.IncludedViaParent;

        if (modEntry.IsDirectory)
        {
            RemoveDirectoryMappingRecursive(modEntry);
        }
        else
        {
            RemoveFileMapping(modEntry);
        }

        // Check if parent node can be unmapped as well
        if (wasMappedViaParent)
        {
            RemoveParentMappingIfNecessary(modEntry);
        }
    }

    private void RemoveDirectoryMappingRecursive(IModContentTreeEntryViewModel modEntry)
    {
        if (!modEntry.IsDirectory)
            return;

        var previewEntry = modEntry.Mapping is null
            ? null
            : PreviewViewModel.TreeEntriesCache.Lookup(modEntry.Mapping.Value).ValueOrDefault();

        var modNode = ModContentViewModel.Root.FindNode(modEntry.RelativePath)!;

        foreach (var child in modNode.Children.Where(child =>
                     child.Item.Status == ModContentTreeEntryStatus.IncludedViaParent))
        {
            if (child.Item.IsDirectory)
            {
                RemoveDirectoryMappingRecursive(child.Item);
            }
            else
            {
                RemoveFileMapping(child.Item);
            }
        }

        modEntry.RemoveMapping();
        if (previewEntry is null) return;

        previewEntry.RemoveDirectoryMapping(modEntry);

        RemovePreviewNodeIfNecessary(previewEntry);
    }

    private void RemoveFileMapping(IModContentTreeEntryViewModel modEntry)
    {
        if (modEntry.IsDirectory)
            return;
        DeploymentData.RemoveMapping(modEntry.RelativePath);

        var previewEntry = modEntry.Mapping is null
            ? null
            : PreviewViewModel.TreeEntriesCache.Lookup(modEntry.Mapping.Value).ValueOrDefault();
        modEntry.RemoveMapping();

        if (previewEntry is null) return;

        previewEntry.RemoveFileMapping();
        PreviewViewModel.TreeEntriesCache.Remove(previewEntry!);
    }

    private void RemovePreviewNodeIfNecessary(IPreviewTreeEntryViewModel previewEntry)
    {
        // Only remove node if it doesn't have other children left
        var previewNode = PreviewViewModel.TreeRoots
            .First(root => root.Item.GamePath.LocationId == previewEntry.GamePath.LocationId)
            .FindNode(previewEntry.GamePath)!;

        if (previewNode.Children.Count == 0)
        {
            PreviewViewModel.TreeEntriesCache.Remove(previewNode.Item);
        }
    }

    private void RemoveParentMappingIfNecessary(IModContentTreeEntryViewModel childEntry)
    {
        while (true)
        {
            // Parent exists since the child was mapped via parent
            var parent = ModContentViewModel.Root.FindNode(childEntry.Parent)!;
            if (parent.Children.Any(x => x.Item.Status == ModContentTreeEntryStatus.IncludedViaParent)) return;

            // Parent needs to be unmapped
            var previewEntry = parent.Item.Mapping.HasValue
                ? PreviewViewModel.TreeEntriesCache.Lookup(parent.Item.Mapping.Value).ValueOrDefault()
                : null;

            if (previewEntry != null)
            {
                previewEntry.RemoveDirectoryMapping(parent.Item);
                RemovePreviewNodeIfNecessary(previewEntry);
            }

            var parentWasMappedViaParent = parent.Item.Status == ModContentTreeEntryStatus.IncludedViaParent;
            parent.Item.RemoveMapping();

            if (parentWasMappedViaParent)
            {
                childEntry = parent.Item;
                continue;
            }

            break;
        }
    }

    private void CleanupPreviewTree(GamePath removedMappingPath)
    {
        if (removedMappingPath.LocationId == LocationId.Unknown)
        {
            // Shouldn't happen
            return;
        }

        foreach (var subPath in removedMappingPath.GetAllParents())
        {
            var previewEntry = PreviewViewModel.TreeEntriesCache.Lookup(subPath);
            if (!previewEntry.HasValue) continue;

            var previewNode = PreviewViewModel.TreeRoots
                .First(root => root.Item.GamePath.LocationId == previewEntry.Value.GamePath.LocationId)
                .FindNode(previewEntry.Value.GamePath)!;

            if (previewNode.Children.Count != 0)
            {
                break;
            }

            PreviewViewModel.TreeEntriesCache.Remove(subPath);
        }
    }

    #endregion RemoveMappingFunctionality

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
