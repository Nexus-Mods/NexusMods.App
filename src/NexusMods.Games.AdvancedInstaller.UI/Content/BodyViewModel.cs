using System.Reactive.Disposables;
using DynamicData;
using DynamicData.Binding;
using DynamicData.Kernel;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Extensions;
using NexusMods.Games.AdvancedInstaller.UI.EmptyPreview;
using NexusMods.Games.AdvancedInstaller.UI.ModContent;
using NexusMods.Games.AdvancedInstaller.UI.Preview;
using NexusMods.Games.AdvancedInstaller.UI.SelectLocation;
using NexusMods.Paths;
using NexusMods.Paths.Trees;
using NexusMods.Sdk.Models.Library;
using NexusMods.UI.Sdk;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.AdvancedInstaller.UI;

using PreviewTreeNode = TreeNodeVM<IPreviewTreeEntryViewModel, GamePath>;
using ModContentTreeNode = TreeNodeVM<IModContentTreeEntryViewModel, RelativePath>;

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

    /// <summary>
    /// Constructor.
    /// </summary>
    public BodyViewModel(
        DeploymentData data,
        string title,
        KeyedBox<RelativePath, LibraryArchiveTree> archiveFiles,
        Loadout.ReadOnly loadout)
    {
        // Setup child VMs
        ModName = title;
        DeploymentData = data;
        CanInstall = false;

        EmptyPreviewViewModel = new EmptyPreviewViewModel();
        ModContentViewModel = new ModContentViewModel(archiveFiles);
        SelectLocationViewModel = new SelectLocationViewModel(loadout);
        PreviewViewModel = new PreviewViewModel();
        CurrentRightContentViewModel = EmptyPreviewViewModel;

        // Setup functionality
        this.WhenActivated(disposables =>
        {
            // Handle user selecting mod content entries
            ModContentViewModel.ModContentEntriesCache.Connect()
                .MergeManyItems(entry => entry.BeginSelectCommand)
                .SubscribeWithErrorLogging(entry => OnBeginSelect(entry.Item))
                .DisposeWith(disposables);

            // Handle user cancelling the selection of mod content entries
            ModContentViewModel.ModContentEntriesCache.Connect()
                .MergeManyItems(entry => entry.CancelSelectCommand)
                .SubscribeWithErrorLogging(entry => OnCancelSelect(entry.Item))
                .DisposeWith(disposables);

            // Handle starting to create a new folder
            SelectLocationViewModel.TreeEntriesCache.Connect()
                .MergeManyItems(entry => entry.EditCreateFolderCommand)
                .SubscribeWithErrorLogging(entry => OnEditCreateFolder(entry.Item))
                .DisposeWith(disposables);

            // Handle Canceling the creation of a new folder
            SelectLocationViewModel.TreeEntriesCache.Connect()
                .MergeManyItems(entry => entry.CancelCreateFolderCommand)
                .SubscribeWithErrorLogging(entry => OnCancelCreateFolder(entry.Item))
                .DisposeWith(disposables);

            // Handle Saving the creation of a new folder
            SelectLocationViewModel.TreeEntriesCache.Connect()
                .MergeManyItems(entry => entry.SaveCreatedFolderCommand)
                .SubscribeWithErrorLogging(entry => OnSaveCreateFolder(entry.Item))
                .DisposeWith(disposables);

            // Handle Deleting the creation of a new folder
            SelectLocationViewModel.TreeEntriesCache.Connect()
                .MergeManyItems(entry => entry.DeleteCreatedFolderCommand)
                .SubscribeWithErrorLogging(entry => OnDeleteCreatedFolder(entry.Item))
                .DisposeWith(disposables);

            // Handle CreateMappingCommand from SelectTreeEntry
            SelectLocationViewModel.TreeEntriesCache.Connect()
                .MergeManyItems(entry => entry.CreateMappingCommand)
                .SubscribeWithErrorLogging(entry => OnCreateMapping(entry.Item))
                .DisposeWith(disposables);

            // Handle CreateMappingCommand from PreviewTreeEntry
            SelectLocationViewModel.SuggestedEntries.ToObservableChangeSet(entry => entry.Id)
                .MergeManyItems(entry => entry.CreateMappingCommand)
                .SubscribeWithErrorLogging(entry => OnCreateMappingFromSuggestedEntry(entry.Item))
                .DisposeWith(disposables);

            // Handle RemoveMappingCommand from PreviewTreeEntry
            PreviewViewModel.TreeEntriesCache.Connect()
                .MergeManyItems(entry => entry.RemoveMappingCommand)
                .SubscribeWithErrorLogging(entry => OnRemoveEntryFromPreview(entry.Item))
                .DisposeWith(disposables);

            // Handle RemoveMappingCommand from ModContentTreeEntry
            ModContentViewModel.ModContentEntriesCache.Connect()
                .MergeManyItems(entry => entry.RemoveMappingCommand)
                .SubscribeWithErrorLogging(entry => OnRemoveMappingFromModContent(entry.Item))
                .DisposeWith(disposables);

            // Update CanInstall when the PreviewViewModel changes
            PreviewViewModel.TreeRoots.WhenAnyValue(roots => roots.Count)
                .SubscribeWithErrorLogging(count => { CanInstall = count > 0; })
                .DisposeWith(disposables);
        });
    }

    #region ModContentSelectionFunctionality

    /// <summary>
    /// Recursively update the mod content tree nodes state to be selected.
    /// Update the SelectedEntriesCache.
    /// Changes the right content view.
    /// </summary>
    /// <param name="modContentTreeEntryViewModel"></param>
    internal void OnBeginSelect(IModContentTreeEntryViewModel modContentTreeEntryViewModel)
    {
        var foundNode = ModContentViewModel.Root.GetTreeNode(modContentTreeEntryViewModel.RelativePath);
        if (!foundNode.HasValue)
            return;

        // Set the status of the parent node to Selecting
        foundNode.Value.Item.Status = ModContentTreeEntryStatus.Selecting;

        // Set the status of all the children to SelectingViaParent
        ModContentViewModel.SelectChildrenRecursive(foundNode.Value);
        ModContentViewModel.SelectedEntriesCache.AddOrUpdate(modContentTreeEntryViewModel);

        // Expand the node
        foundNode.Value.Item.IsExpanded = true;

        // Update the UI to show the SelectLocationViewModel
        CurrentRightContentViewModel = SelectLocationViewModel;
    }

    /// <summary>
    /// Recursively deselects the mod content entry and children.
    /// Removes entries from SelectedEntriesCache.
    /// Potentially deselects parent if no more children are selected.
    /// Potentially changes the right content view.
    /// </summary>
    /// <param name="modContentTreeEntryViewModel">The mod content entry to deselect.</param>
    internal void OnCancelSelect(IModContentTreeEntryViewModel modContentTreeEntryViewModel)
    {
        var foundNode = ModContentViewModel.Root.GetTreeNode(modContentTreeEntryViewModel.RelativePath);
        if (!foundNode.HasValue)
            return;

        var oldStatus = foundNode.Value.Item.Status;

        // Set the status of the parent node to Default
        foundNode.Value.Item.Status = ModContentTreeEntryStatus.Default;

        // Set the status of all the children to Default
        ModContentViewModel.DeselectChildrenRecursive(foundNode.Value);
        ModContentViewModel.SelectedEntriesCache.Remove(modContentTreeEntryViewModel);

        // Check if ancestors need to be cleaned up (no more selected children).
        if (oldStatus == ModContentTreeEntryStatus.SelectingViaParent)
        {
            var parent = foundNode.Value.Parent;
            while (parent.HasValue)
            {
                if (parent.Value.Children.Any(x =>
                        x.Item.Status == ModContentTreeEntryStatus.SelectingViaParent))
                    break;
                var prevStatus = parent.Value.Item.Status;

                // If no more children are selected, reset the status of the parent node to Default
                parent.Value.Item.Status = ModContentTreeEntryStatus.Default;

                if (prevStatus == ModContentTreeEntryStatus.Selecting)
                {
                    // If the parent node was Selecting, remove it from the SelectedEntriesCache and stop
                    ModContentViewModel.SelectedEntriesCache.Remove(parent.Value.Item);
                    break;
                }

                // This was a SelectingViaParent node as well, check the next parent
                parent = parent.Value.Parent;
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

    /// <summary>
    /// Creates a mapping for the selected mod contents to the selected suggested location.
    /// </summary>
    /// <param name="suggestedEntry">Suggested location entry.</param>
    internal void OnCreateMappingFromSuggestedEntry(ISuggestedEntryViewModel suggestedEntry)
    {
        // Find the corresponding Selectable tree entry, and create a mapping using that.
        var correspondingTreeEntry = SelectLocationViewModel.TreeEntriesCache
            .Lookup(suggestedEntry.RelativeToTopLevelLocation).ValueOrDefault();

        if (correspondingTreeEntry is not null)
            OnCreateMapping(correspondingTreeEntry);
    }

    /// <summary>
    /// Recursively map the selected mod contents inside the target folder.
    /// Changes the right content view.
    /// </summary>
    /// <param name="selectableTreeEntryViewModel">Selectable tree folder under which the mod files need to be mapped.</param>
    internal void OnCreateMapping(ISelectableTreeEntryViewModel selectableTreeEntryViewModel)
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
                    selectedModEntry.SetFileMapping(mappingParentPreviewEntry, mappingParentPreviewEntry.DisplayName,
                        true);
                    mappingParentPreviewEntry.MappedEntries.Add(selectedModEntry);

                    MapChildrenRecursive(ModContentViewModel.Root, mappingParentPreviewEntry, previewTreeUpdater);
                    continue;
                }

                var entryMappingPath = new GamePath(targetLocation.GamePath.LocationId,
                    targetLocation.GamePath.Path.Join(selectedModEntry.RelativePath.FileName));
                var previewEntry = previewTreeUpdater.Lookup(entryMappingPath).ValueOrDefault();

                if (selectedModEntry.IsDirectory)
                {
                    var selectedModNode = ModContentViewModel.Root.GetTreeNode(selectedModEntry.RelativePath);
                    if (!selectedModNode.HasValue)
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

                    CreateDirectoryMapping(selectedModNode.Value, previewEntry, previewTreeUpdater, true);
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

        ExpandPreviewNodesInPath(targetLocation.GamePath);
        PreviewViewModel.TreeRoots.GetTreeNode(targetLocation.GamePath).IfHasValue(ExpandPreviewNodesToMatchModContent);

        CurrentRightContentViewModel = PreviewViewModel;
    }

    /// <summary>
    /// Recursively maps a mod content directory to a preview tree entry.
    /// </summary>
    /// <param name="sourceNode"></param>
    /// <param name="destPreviewEntry"></param>
    /// <param name="previewTreeUpdater"></param>
    /// <param name="isExplicit"></param>
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

    /// <summary>
    /// Creates a mapping between a mod content file and a preview tree entry.
    /// </summary>
    /// <param name="sourceEntry">source ModContent entry</param>
    /// <param name="destPreviewEntry">Destination Preview entry</param>
    /// <param name="isExplicit">Whether this was explicitly mapped or mapped through a parent.</param>
    private void CreateFileMapping(IModContentTreeEntryViewModel sourceEntry,
        IPreviewTreeEntryViewModel destPreviewEntry, bool isExplicit)
    {
        destPreviewEntry.AddMapping(sourceEntry);
        var mappingFolderName = PreviewViewModel.TreeEntriesCache.Lookup(destPreviewEntry.Parent)
            .ValueOrDefault()?.DisplayName;
        sourceEntry.SetFileMapping(destPreviewEntry, mappingFolderName ?? string.Empty, isExplicit);
        DeploymentData.AddMapping(sourceEntry.RelativePath, destPreviewEntry.GamePath);
    }

    /// <summary>
    /// Removes a previous file mapping from the preview tree entry if present.
    /// This is in case the user creates a mapping to an already mapped location.
    /// So previous mapping needs to be removed.
    /// </summary>
    /// <param name="previewEntry">The preview entry from which to remove the mapping.</param>
    private void RemovePreviousFileMapping(IPreviewTreeEntryViewModel previewEntry)
    {
        if (!previewEntry.MappedEntry.HasValue)
            return;
        var modEntry = previewEntry.MappedEntry.Value;
        var mappedViaParent = modEntry.Status == ModContentTreeEntryStatus.IncludedViaParent;

        previewEntry.RemoveFileMapping();
        modEntry.RemoveMapping();

        if (mappedViaParent)
        {
            RemoveParentMappingIfNecessary(modEntry, false);
        }

        DeploymentData.RemoveMapping(modEntry.RelativePath);
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

    /// <summary>
    /// This goes through each preview node in a subTree and sets the expanded state to match the one in the mod content tree.
    /// </summary>
    private void ExpandPreviewNodesToMatchModContent(PreviewTreeNode subTree)
    {
        foreach (var child in subTree.Children)
        {
            if (!child.Item.IsDirectory)
                continue;

            if (!child.Item.MappedEntries.Any(modEntry =>
                    ModContentViewModel.Root.GetTreeNode(modEntry.RelativePath).ValueOrDefault() is
                    {
                        Item.IsExpanded: true,
                    })) continue;

            // At least one of the mapped entries is expanded in the modContent tree, so we expand this node as well.
            child.Item.IsExpanded = true;
            ExpandPreviewNodesToMatchModContent(child);
        }
    }

    /// <summary>
    /// Expands all the preview nodes from the root to the node with the given path.
    /// </summary>
    /// <param name="path"></param>
    private void ExpandPreviewNodesInPath(GamePath path)
    {
        var previewNode = PreviewViewModel.TreeRoots.GetTreeNode(path).ValueOrDefault();
        if (previewNode is null)
            return;

        previewNode.Item.IsExpanded = true;

        while (previewNode.Parent.HasValue)
        {
            previewNode.Parent.Value.Item.IsExpanded = true;
            previewNode = previewNode.Parent.Value;
        }
    }

    /// <summary>
    /// Creates mappings for all children of the given Mod content directory.
    /// </summary>
    /// <param name="sourceNode">Source mod content folder.</param>
    /// <param name="mappingParentPreviewNode">The folder under which all the children are to be mapped.</param>
    /// <param name="previewTreeUpdater">An updater object to be used to add new preview entries to the preview tree.
    ///     This will result in a single batch update in the end.
    /// </param>
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

    /// <summary>
    /// Removes the passed entry and all its children from the preview tree and removes all associated mappings.
    /// Cleans up the preview tree if necessary.
    /// Potentially changes the right content view.
    /// </summary>
    /// <param name="previewEntry">The preview entry the user requested to remove.</param>
    internal void OnRemoveEntryFromPreview(IPreviewTreeEntryViewModel previewEntry)
    {
        RemoveEntryFromPreview(previewEntry);
        CleanupPreviewTree(previewEntry.GamePath);

        // Switch to empty view if nothing is mapped anymore
        CurrentRightContentViewModel = PreviewViewModel.TreeEntriesCache.Count > 0
            ? PreviewViewModel
            : EmptyPreviewViewModel;
    }

    /// <summary>
    /// Removes the passed entry and all its children from the preview tree and removes all associated mappings.
    /// </summary>
    /// <param name="previewEntry"></param>
    private void RemoveEntryFromPreview(IPreviewTreeEntryViewModel previewEntry)
    {
        if (previewEntry.IsDirectory)
        {
            RemoveDirectoryFromPreviewRecursive(previewEntry);
        }
        else
        {
            RemoveFileFromPreview(previewEntry);
        }
    }

    /// <summary>
    /// Removes the passed directory and all its children from the preview tree and removes all associated mappings.
    /// </summary>
    /// <param name="previewEntry"></param>
    private void RemoveDirectoryFromPreviewRecursive(IPreviewTreeEntryViewModel previewEntry)
    {
        if (!previewEntry.IsDirectory)
            return;

        foreach (var mappedEntry in previewEntry.MappedEntries.ToArray())
        {
            // Will also remove this preview node.
            StartRemoveMapping(mappedEntry);
        }

        // Check if the node still exists and has any children left
        var previewNode = PreviewViewModel.TreeRoots.GetTreeNode(previewEntry.GamePath);

        if (previewNode.HasValue && previewNode.Value.Children.Count != 0)
        {
            // User removed the item from preview, we need force remove all children
            foreach (var child in previewNode.Value.Children.ToArray())
            {
                RemoveEntryFromPreview(child.Item);
            }
        }

        // Now that all descendent have been properly unmapped, we can remove this node.
        PreviewViewModel.TreeEntriesCache.Remove(previewEntry);
    }

    /// <summary>
    /// Removes the passed file from the preview tree and removes the associated mapping.
    /// </summary>
    /// <param name="previewEntry"></param>
    private void RemoveFileFromPreview(IPreviewTreeEntryViewModel previewEntry)
    {
        if (!previewEntry.MappedEntry.HasValue)
        {
            previewEntry.RemoveFileMapping();
            PreviewViewModel.TreeEntriesCache.Remove(previewEntry);
            return;
        }

        // Will also remove this preview node.
        StartRemoveMapping(previewEntry.MappedEntry.Value);
    }

    /// <summary>
    /// Removes the mappings from the ModContent entry and all its included children.
    /// Will remove preview nodes associated with the mappings that have no other mappings.
    /// Will remove mappings from parent nodes if no mapped children are left.
    /// Potentially changes the right content view.
    /// </summary>
    /// <param name="modEntry"></param>
    internal void OnRemoveMappingFromModContent(IModContentTreeEntryViewModel modEntry)
    {
        var mappingPath = modEntry.Mapping.ValueOr(new GamePath(LocationId.Unknown, RelativePath.Empty));

        StartRemoveMapping(modEntry);
        CleanupPreviewTree(mappingPath);

        // Switch to empty view if nothing is mapped anymore
        CurrentRightContentViewModel = PreviewViewModel.TreeEntriesCache.Count > 0
            ? PreviewViewModel
            : EmptyPreviewViewModel;
    }

    /// <summary>
    /// Removes the mapping from the ModContent entry and all its included children.
    /// Will remove preview nodes associated with the mappings that have no other mappings.
    /// Will remove mappings from parent nodes if no mapped children are left.
    /// </summary>
    /// <param name="modEntry"></param>
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

    /// <summary>
    /// Removes the mapping from the ModContent directory and all its included children.
    /// Will remove preview nodes associated with the mappings that have no other mappings.
    /// </summary>
    /// <param name="modEntry"></param>
    private void RemoveDirectoryMappingRecursive(IModContentTreeEntryViewModel modEntry)
    {
        if (!modEntry.IsDirectory)
            return;

        var previewEntry = modEntry.Mapping.HasValue
            ? PreviewViewModel.TreeEntriesCache.Lookup(modEntry.Mapping.Value).ValueOrDefault()
            : null;

        var modNode = ModContentViewModel.Root.GetTreeNode(modEntry.RelativePath).Value;

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

    /// <summary>
    /// Removes a file mapping from the ModContent entry and the associated preview entry.
    /// Will remove the preview entry from the preview tree.
    /// </summary>
    /// <param name="modEntry"></param>
    private void RemoveFileMapping(IModContentTreeEntryViewModel modEntry)
    {
        if (modEntry.IsDirectory)
            return;
        DeploymentData.RemoveMapping(modEntry.RelativePath);

        var previewEntry = modEntry.Mapping.HasValue
            ? PreviewViewModel.TreeEntriesCache.Lookup(modEntry.Mapping.Value).ValueOrDefault()
            : null;

        modEntry.RemoveMapping();

        if (previewEntry is null) return;

        previewEntry.RemoveFileMapping();
        PreviewViewModel.TreeEntriesCache.Remove(previewEntry);
    }

    /// <summary>
    /// Removes the preview node if it is doesn't have any children left.
    /// </summary>
    /// <param name="previewEntry"></param>
    private void RemovePreviewNodeIfNecessary(IPreviewTreeEntryViewModel previewEntry)
    {
        // Only remove node if it doesn't have other children left
        var previewNode = PreviewViewModel.TreeRoots.GetTreeNode(previewEntry.GamePath).Value;

        if (previewNode.Children.Count == 0)
        {
            PreviewViewModel.TreeEntriesCache.Remove(previewNode.Item);
        }
    }

    /// <summary>
    /// Removes the mapping from the mod content parent node if none of its children are mapped anymore.
    /// </summary>
    /// <param name="childEntry">The child of the parent node to remove the mapping from.</param>
    /// <param name="removePreviewNodes">Whether to also remove the associated preview entry from the preview tree.</param>
    private void RemoveParentMappingIfNecessary(IModContentTreeEntryViewModel childEntry,
        bool removePreviewNodes = true)
    {
        var currentParent = ModContentViewModel.Root.GetTreeNode(childEntry.RelativePath).ValueOrDefault()?.Parent
            .ValueOrDefault();
        if (currentParent is null) return;

        while (true)
        {
            // Parent exists since the child was mapped via parent
            if (currentParent.Children.Any(x => x.Item.Status == ModContentTreeEntryStatus.IncludedViaParent)) return;

            // Parent needs to be unmapped
            var previewEntry = currentParent.Item.Mapping.HasValue
                ? PreviewViewModel.TreeEntriesCache.Lookup(currentParent.Item.Mapping.Value).ValueOrDefault()
                : null;

            if (previewEntry != null)
            {
                previewEntry.RemoveDirectoryMapping(currentParent.Item);
                if (removePreviewNodes)
                    RemovePreviewNodeIfNecessary(previewEntry);
            }

            var parentWasMappedViaParent = currentParent.Item.Status == ModContentTreeEntryStatus.IncludedViaParent;
            currentParent.Item.RemoveMapping();

            if (parentWasMappedViaParent)
            {
                currentParent = currentParent.Parent.ValueOrDefault();
                if (currentParent is null) return;
                continue;
            }

            break;
        }
    }

    /// <summary>
    /// Given the GamePath of a removed preview entry, this function will remove any nodes on the path that have no children left.
    /// </summary>
    /// <param name="removedMappingPath"></param>
    private void CleanupPreviewTree(GamePath removedMappingPath)
    {
        if (removedMappingPath.LocationId == LocationId.Unknown)
        {
            // Shouldn't happen
            return;
        }

        // The nested paths might be missing, so try all subPaths
        foreach (var subPath in removedMappingPath.GetAllParents())
        {
            var previewEntry = PreviewViewModel.TreeEntriesCache.Lookup(subPath);
            if (!previewEntry.HasValue) continue;

            var previewNode = PreviewViewModel.TreeRoots.GetTreeNode(previewEntry.Value.GamePath).Value;

            if (previewNode.Children.Count != 0)
            {
                break;
            }

            PreviewViewModel.TreeEntriesCache.Remove(previewNode.Id);
        }
    }

    #endregion RemoveMappingFunctionality

    #region CreateFolderFunctionality

    /// <summary>
    /// Shows the input box for typing the name of the new folder in Selectable tree.
    /// </summary>
    /// <param name="selectableTreeEntryViewModel"></param>
    private void OnEditCreateFolder(ISelectableTreeEntryViewModel selectableTreeEntryViewModel)
    {
        var foundNode = SelectLocationViewModel.TreeRoots.GetTreeNode(selectableTreeEntryViewModel.GamePath)
            .ValueOrDefault();
        if (foundNode is null)
            return;

        foundNode.Item.InputText = string.Empty;

        // Set the status of the parent node to Edit
        foundNode.Item.Status = SelectableDirectoryNodeStatus.Editing;
    }


    /// <summary>
    /// Cancels the creation of a new folder in Selectable tree.
    /// </summary>
    /// <param name="selectableTreeEntryViewModel"></param>
    private void OnCancelCreateFolder(ISelectableTreeEntryViewModel selectableTreeEntryViewModel)
    {
        var foundNode = SelectLocationViewModel.TreeRoots.GetTreeNode(selectableTreeEntryViewModel.GamePath)
            .ValueOrDefault();
        if (foundNode is null)
            return;

        // Reset the status to Create
        foundNode.Item.Status = SelectableDirectoryNodeStatus.Create;
        foundNode.Item.InputText = string.Empty;
    }

    /// <summary>
    /// Creates a new folder in Selectable tree from the InputField text.
    /// </summary>
    /// <param name="selectableTreeEntryViewModel"></param>
    private void OnSaveCreateFolder(ISelectableTreeEntryViewModel selectableTreeEntryViewModel)
    {
        var foundNode = SelectLocationViewModel.TreeRoots
            .GetTreeNode(selectableTreeEntryViewModel.GamePath).ValueOrDefault();
        if (foundNode is null)
            return;

        var folderName = foundNode.Item.GetSanitizedInput();
        if (folderName == RelativePath.Empty)
            return;

        var newPath = new GamePath(foundNode.Item.GamePath.LocationId,
            foundNode.Item.GamePath.Parent.Path.Join(folderName));

        // If it doesn't exist yet, create a new node
        if (foundNode.Parent.Value.Children.All(child => child.Item.GamePath != newPath))
        {
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
        }

        // Reset this to Create state.
        foundNode.Item.Status = SelectableDirectoryNodeStatus.Create;
        foundNode.Item.InputText = string.Empty;

        // Expand the new node
        SelectLocationViewModel.TreeRoots.GetTreeNode(newPath).Value.Item.IsExpanded = true;
    }

    /// <summary>
    /// Will remove the created Selectable tree entry and all its created children.
    /// </summary>
    /// <param name="selectableTreeEntryViewModel"></param>
    private void OnDeleteCreatedFolder(ISelectableTreeEntryViewModel selectableTreeEntryViewModel)
    {
        var foundNode = SelectLocationViewModel.TreeRoots
            .GetTreeNode(selectableTreeEntryViewModel.GamePath).ValueOrDefault();
        if (foundNode is null) return;

        // Remove the node and all children from the cache
        var idsToRemove = foundNode.GetAllDescendentIds().Append(foundNode.Item.GamePath);
        SelectLocationViewModel.TreeEntriesCache.Remove(idsToRemove);
    }

    #endregion CreateFolderFunctionality
}
