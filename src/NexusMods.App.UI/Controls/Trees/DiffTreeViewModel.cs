using System.Collections.ObjectModel;
using Avalonia.Controls;
using DynamicData;
using Humanizer.Bytes;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.Trees;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.App.UI.Controls.Trees.Files;
using NexusMods.App.UI.Helpers.TreeDataGrid;
using NexusMods.App.UI.Resources;
using NexusMods.Paths.Trees.Traits;

namespace NexusMods.App.UI.Controls.Trees;

public class DiffTreeViewModel : AViewModel<IFileTreeViewModel>, IFileTreeViewModel
{
    private readonly LoadoutId _loadoutId;
    private readonly IApplyService _applyService;
    private readonly ILoadoutRegistry _loadoutRegistry;
    private readonly SourceCache<IFileTreeNodeViewModel, GamePath> _treeSourceCache;
    private readonly ReadOnlyObservableCollection<IFileTreeNodeViewModel> _items;

    private readonly ReadOnlyObservableCollection<string> _statusBarStrings;
    private readonly SourceList<string> _statusBarSourceList;

    public ITreeDataGridSource<IFileTreeNodeViewModel> TreeSource { get; }
    public ReadOnlyObservableCollection<string> StatusBarStrings => _statusBarStrings;


    public DiffTreeViewModel(LoadoutId loadoutId, IApplyService applyService, ILoadoutRegistry loadoutRegistry)
    {
        _loadoutId = loadoutId;
        _applyService = applyService;
        _loadoutRegistry = loadoutRegistry;

        _treeSourceCache = new SourceCache<IFileTreeNodeViewModel, GamePath>(entry => entry.Key);
        _statusBarSourceList = new SourceList<string>();

        _treeSourceCache.Connect()
            .TransformToTree(model => model.ParentKey)
            .Transform(node => node.Item.Initialize(node))
            .OnUI()
            .Bind(out _items)
            .Subscribe();

        _statusBarSourceList.Connect()
            .OnUI()
            .Bind(out _statusBarStrings)
            .Subscribe();

        TreeSource = CreateTreeSource(_items);
    }

    public async Task Refresh()
    {
        var loadout = _loadoutRegistry.Get(_loadoutId);
        if (loadout is null)
        {
            throw new KeyNotFoundException($"Loadout with ID {_loadoutId} not found.");
        }

        var diffTree = await _applyService.GetApplyDiffTree(_loadoutId);

        Dictionary<GamePath, IFileTreeNodeViewModel> fileViewModelNodes = [];

        uint loadoutFileCount = 0;
        ulong loadoutFileSize = 0;
        uint diskFileCount = 0;
        ulong diskFileSize = 0;
        uint addedFileCount = 0;
        uint modifiedFileCount = 0;
        uint deletedFileCount = 0;
        ulong operationSize = 0;


        var locationsRegister = loadout.Installation.LocationsRegister;

        // Add the root directories
        foreach (var rootNode in diffTree.GetRoots())
        {
            var model = new FileTreeNodeViewModel(
                locationsRegister.GetResolvedPath(rootNode.GamePath()).ToString(),
                rootNode.GamePath(),
                IFileTreeViewModel.RootParentGamePath,
                false,
                0ul,
                0u
            )
            {
                ChangeType = FileChangeType.None,
            };
            fileViewModelNodes.Add(rootNode.GamePath(), model);
        }

        // Add all the sub directories recursively
        foreach (var diffEntry in diffTree.GetAllDescendentDirectories())
        {
            var model = new FileTreeNodeViewModel(
                diffEntry.GamePath(),
                diffEntry.Parent()?.GamePath() ?? IFileTreeViewModel.RootParentGamePath,
                diffEntry.IsFile(),
                diffEntry.Item.Value.Size.Value,
                0u,
                diffEntry.Item.Value.ChangeType
            )
            {
                IsExpanded = diffEntry.Item.Value.ChangeType != FileChangeType.None,
            };
            fileViewModelNodes.Add(diffEntry.GamePath(), model);
        }

        // Add all files 
        foreach (var diffEntry in diffTree.GetAllDescendentFiles())
        {
            var gamePath = diffEntry.GamePath();
            var parentPath = diffEntry.Parent()?.GamePath() ?? IFileTreeViewModel.RootParentGamePath;
            var fileSize = diffEntry.Item.Value.Size.Value;
            var changeType = diffEntry.Item.Value.ChangeType;
            var model = new FileTreeNodeViewModel(gamePath,
                parentPath,
                diffEntry.IsFile(),
                fileSize,
                0u,
                changeType
            );

            // Update all parent folders with the file size and file count
            foreach (var parent in gamePath.GetAllParents().Skip(1))
            {
                var parentNode = fileViewModelNodes[parent];
                parentNode.FileSize += fileSize;
                parentNode.FileCount++;

                // If file is changed and parent is not changed, mark parent as modified
                if (changeType == FileChangeType.None || parentNode.ChangeType != FileChangeType.None)
                    continue;
                parentNode.ChangeType = FileChangeType.Modified;
                parentNode.IsExpanded = true;
            }

            // Update the counters
            if (changeType != FileChangeType.Removed)
            {
                // File is part of loadout
                loadoutFileCount++;
                loadoutFileSize += fileSize;
            }

            if (changeType != FileChangeType.Added)
            {
                // File is part of disk
                diskFileCount++;
                diskFileSize += fileSize;
            }

            if (changeType != FileChangeType.None)
                operationSize += fileSize;

            addedFileCount += changeType == FileChangeType.Added ? 1u : 0u;
            modifiedFileCount += changeType == FileChangeType.Modified ? 1u : 0u;
            deletedFileCount += changeType == FileChangeType.Removed ? 1u : 0u;

            fileViewModelNodes.Add(gamePath, model);
        }

        _treeSourceCache.Edit(innerList =>
            {
                innerList.Clear();
                innerList.AddOrUpdate(fileViewModelNodes);
            }
        );

        _statusBarSourceList.Edit(innerList =>
            {
                innerList.Clear();
                innerList.Add(string.Format(Language.DiffTreeViewModel_StatusBar__File_Loadout_Disk,
                        loadoutFileCount,
                        ByteSize.FromBytes(loadoutFileSize).ToString(),
                        diskFileCount,
                        ByteSize.FromBytes(diskFileSize).ToString()
                    )
                );
                innerList.Add(string.Format(Language.DiffTreeViewModel_StatusBar__Apply_changes,
                        addedFileCount,
                        modifiedFileCount,
                        deletedFileCount
                    )
                );
                innerList.Add(string.Format(Language.DiffTreeViewModel_StatusBar__Data_to_process___0_,
                        ByteSize.FromBytes(operationSize).ToString()
                    )
                );
            }
        );
    }

    internal static HierarchicalTreeDataGridSource<IFileTreeNodeViewModel> CreateTreeSource(
        ReadOnlyObservableCollection<IFileTreeNodeViewModel> treeRoots)
    {
        return new HierarchicalTreeDataGridSource<IFileTreeNodeViewModel>(treeRoots)
        {
            Columns =
            {
                FileTreeNodeViewModel.CreateTreeSourceNameColumn(),
                FileTreeNodeViewModel.CreateTreeSourceStateColumn(),
                FileTreeNodeViewModel.CreateTreeSourceFileCountColumn(),
                FileTreeNodeViewModel.CreateTreeSourceSizeColumn(),
            },
        };
    }
}
