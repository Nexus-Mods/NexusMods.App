using System.Collections.ObjectModel;
using Avalonia.Controls;
using DynamicData;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.Trees;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.App.UI.Controls.Trees.Files;
using NexusMods.App.UI.Helpers.TreeDataGrid;
using NexusMods.Paths.Trees.Traits;

namespace NexusMods.App.UI.Controls.Trees;

public class DiffTreeViewModel : AViewModel<IFileTreeViewModel>, IFileTreeViewModel
{
    private LoadoutId _loadoutId;
    private IApplyService _applyService;
    private ILoadoutRegistry _loadoutRegistry;
    private readonly SourceCache<IFileTreeNodeViewModel, GamePath> _treeSourceCache;
    private ReadOnlyObservableCollection<IFileTreeNodeViewModel> _items;

    private uint _loadoutFileCount;
    private ulong _loadoutFileSize;
    private uint _diskFileCount;
    private ulong _diskFileSize;
    private uint _addedFileCount;
    private uint _modifiedFileCount;
    private uint _deletedFileCount;
    private ulong _operationSize;

    private readonly ReadOnlyObservableCollection<string> _statusBarStrings;
    private SourceList<string> _statusBarSourceList;

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
            
            fileViewModelNodes.Add(gamePath, model);
        }

        _treeSourceCache.Edit(innerList =>
            {
                innerList.Clear();
                innerList.AddOrUpdate(fileViewModelNodes);
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
