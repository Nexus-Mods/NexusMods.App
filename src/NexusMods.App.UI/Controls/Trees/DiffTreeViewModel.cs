using System.Collections.ObjectModel;
using Avalonia.Controls;
using DynamicData;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.Trees;
using NexusMods.Abstractions.Loadouts;
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

        List<IFileTreeNodeViewModel> fileViewModelNodes = [];

        // Get the root nodes:
        var locationsRegister = loadout.Installation.LocationsRegister;
        var rootNodes = diffTree.GetRoots();
        foreach (var rootNode in rootNodes)
        {
            var gamePath = rootNode.GamePath();
            var fullPath = locationsRegister.GetResolvedPath(gamePath);
            var isFile = rootNode.IsFile();
            var fileSize = rootNode.IsFile()
                ? rootNode.Item.Value.Size.Value
                : rootNode.GetFiles()
                    .Aggregate(0ul,
                        (sum, file) => { return sum += file.Item.Value.Size.Value; }
                    );
            var numChildFiles = rootNode.IsFile() ? 0 : rootNode.CountFiles();
            var model = new FileTreeNodeViewModel(fullPath.ToString(),
                gamePath,
                IFileTreeViewModel.RootParentGamePath,
                isFile,
                fileSize,
                (uint)numChildFiles
            );
            fileViewModelNodes.Add(model);
        }


        // Convert the diff tree to a list of FileTreeNodeViewModels
        foreach (var diffEntry in diffTree.GetAllDescendents())
        {
            var gamePath = diffEntry.GamePath();
            var parentPath = diffEntry.Parent()?.GamePath() ?? IFileTreeViewModel.RootParentGamePath;
            var isFile = diffEntry.IsFile();
            var fileSize = diffEntry.IsFile()
                ? diffEntry.Item.Value.Size.Value
                : diffEntry.GetFiles()
                    .Aggregate(0ul,
                        (sum, file) => { return sum += file.Item.Value.Size.Value; }
                    );
            var numChildFiles = diffEntry.IsFile() ? 0 : diffEntry.CountFiles();
            var model = new FileTreeNodeViewModel(gamePath,
                parentPath,
                isFile,
                fileSize,
                (uint)numChildFiles,
                diffEntry.Item.Value.ChangeType
            );
            fileViewModelNodes.Add(model);
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
