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

    private uint _loadoutFileCount;
    private ulong _loadoutFileSize;
    private uint _diskFileCount;
    private ulong _diskFileSize;
    private uint _addedFileCount;
    private uint _modifiedFileCount;
    private uint _deletedFileCount;
    private ulong _operationSize;

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
            .Bind(out _items)
            .Subscribe();

        _statusBarSourceList.Connect()
            .Bind(out _statusBarStrings)
            .Subscribe();

        TreeSource = CreateTreeSource(_items);
    }

    public async void Refresh()
    {
        var loadout = _loadoutRegistry.Get(_loadoutId);
        if (loadout is null)
        {
            throw new KeyNotFoundException($"Loadout with ID {_loadoutId} not found.");
        }

        var diffTree = await _applyService.GetApplyDiffTree(_loadoutId);

        Dictionary<GamePath, IFileTreeNodeViewModel> fileViewModelNodes = [];


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
            );
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
            }

            // Update the counters
            if (changeType != FileChangeType.Removed)
            {
                // File is part of loadout
                _loadoutFileCount++;
                _loadoutFileSize += fileSize;
            }

            if (changeType != FileChangeType.Added)
            {
                // File is part of disk
                _diskFileCount++;
                _diskFileSize += fileSize;
            }

            if (changeType != FileChangeType.None)
                _operationSize += fileSize;

            _addedFileCount += changeType == FileChangeType.Added ? 1u : 0u;
            _modifiedFileCount += changeType == FileChangeType.Modified ? 1u : 0u;
            _deletedFileCount += changeType == FileChangeType.Removed ? 1u : 0u;

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
                        _loadoutFileCount,
                        ByteSize.FromBytes(_loadoutFileSize).ToString(),
                        _diskFileCount,
                        ByteSize.FromBytes(_diskFileSize).ToString()
                    )
                );
                innerList.Add(string.Format(Language.DiffTreeViewModel_StatusBar__Apply_changes,
                        _addedFileCount,
                        _modifiedFileCount,
                        _deletedFileCount
                    )
                );
                innerList.Add(string.Format(Language.DiffTreeViewModel_StatusBar__Data_to_process___0_,
                        ByteSize.FromBytes(_operationSize).ToString()
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
