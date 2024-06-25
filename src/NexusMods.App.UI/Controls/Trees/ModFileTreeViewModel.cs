using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using DynamicData;
using Humanizer.Bytes;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.App.UI.Controls.Trees.Files;
using NexusMods.App.UI.Helpers.TreeDataGrid;
using NexusMods.App.UI.Resources;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.App.UI.Controls.Trees;

/// <summary>
/// This is one implementation of the IFileTreeViewModel, which is used to display a tree of files in a mod.
/// </summary>
public class ModFileTreeViewModel : AViewModel<IFileTreeViewModel>, IFileTreeViewModel
{
    private readonly IConnection _conn;
    private readonly SourceCache<IFileTreeNodeViewModel, GamePath> _sourceCache;
    private ReadOnlyObservableCollection<IFileTreeNodeViewModel> _items;
    private uint _totalNumFiles;
    private ulong _totalSize;
    private ReadOnlyObservableCollection<string> _statusBarStrings;
    private SourceList<string> StatusBarStringCache { get; } = new();

    public ITreeDataGridSource<IFileTreeNodeViewModel> TreeSource { get; }
    public ReadOnlyObservableCollection<string> StatusBarStrings => _statusBarStrings;

    public ModFileTreeViewModel(LoadoutId loadoutId, ModId modId, IConnection conn)
    {
        _conn = conn;
        _items = new ReadOnlyObservableCollection<IFileTreeNodeViewModel>([]);
        _sourceCache = new SourceCache<IFileTreeNodeViewModel, GamePath>(x => x.Key);
        _totalNumFiles = 0;
        _totalSize = 0;

        // Store GamePaths to dedupe the strings. No unsafe API in .NET to access the keys directly, but we need parent anyway, so it's ok.
        var folderToSize = new Dictionary<GamePath, (ulong size, uint numFileChildren, GamePath folder, GamePath parent, bool isLeaf)>();

        var db = _conn.Db;
        var loadout = Loadout.Load(db, loadoutId);
        var locationsRegister = loadout.InstallationInstance.LocationsRegister;
        var mod = Mod.Load(db, modId);
        var displayedItems = new List<IFileTreeNodeViewModel>();

        // Add all the files to the displayedItems list
        foreach (var file in mod.Files)
        {
            // TODO: Check for IStoredFile, IToFile interfaces if we ever have more types of files that get put to disk.
            if (!file.TryGetAsStoredFile(out var storedFile))
                continue;

            var isDeletion = file.TryGetAsDeletedFile(out _);
            _totalNumFiles++;
            
            if (!isDeletion)
                _totalSize += storedFile.Size.Value;

            var folderName = storedFile.AsFile().To.Parent;
            var parent = folderName;

            displayedItems.Add(new FileTreeNodeViewModel(
                    storedFile.AsFile().To,
                    folderName,
                    true,
                    isDeletion ? 0 : storedFile.Size.Value,
                    0,
                    isDeletion
                )
            );

            // Add all the parent folders to the folderToSize dictionary
            while (parent.Path != "")
            {
                ref var item = ref CollectionsMarshal.GetValueRefOrNullRef(folderToSize, parent);
                var exists = !Unsafe.IsNullRef(ref item);
                // TODO: keep track of deleted files in the parent folders, to mark them as deleted if they only contain deletions 
                if (!exists)
                {
                    folderToSize.Add(parent, (storedFile.Size.Value, 1, parent, parent.Parent, false));
                }
                else
                {
                    // We had already added this folder, so just update the size and numChildren
                    item.size += storedFile.Size.Value;
                    item.numFileChildren++;
                }

                parent = parent.Parent;
            }

            // Add the root folder to the folderToSize dictionary
            ref var rootItem = ref CollectionsMarshal.GetValueRefOrNullRef(folderToSize, parent);
            var rootExists = !Unsafe.IsNullRef(ref rootItem);
            if (!rootExists)
            {
                // Root folders have no parent, so we use an invalid GamePath
                folderToSize.Add(parent, (storedFile.Size.Value, 1, parent, IFileTreeViewModel.RootParentGamePath, false));
            }
            else
            {
                // We had already added this folder, so just update the size and numChildren
                rootItem.size += storedFile.Size.Value;
                rootItem.numFileChildren++;
            }
        }

        // Add all the folders in folderToSize to the displayedItems list
        foreach (var (key, value) in folderToSize)
        {
            var (size, numChildren, folder, parent, isLeaf) = value;

            if (parent.Equals(IFileTreeViewModel.RootParentGamePath))
            {
                // Add a root with full path as name
                displayedItems.Add(new FileTreeNodeViewModel(
                        locationsRegister[folder.LocationId].ToString(),
                        folder,
                        parent,
                        false,
                        size,
                        numChildren
                    )
                    {
                        // Always expand the root nodes
                        IsExpanded = true,
                    }
                );
                continue;
            }

            displayedItems.Add(new FileTreeNodeViewModel(
                    folder,
                    parent,
                    isLeaf,
                    size,
                    numChildren
                )
            );
        }

        _sourceCache.Clear();
        _sourceCache.AddOrUpdate(displayedItems);

        _sourceCache.Connect()
            .TransformToTree(model => model.ParentKey)
            .Transform(node => node.Item.Initialize(node))
            .Bind(out _items)
            .Subscribe(); // force evaluation

        TreeSource = CreateTreeSource(_items);
        TreeSource.SortBy(TreeSource.Columns[0], ListSortDirection.Ascending);

        // Populate the status bar
        StatusBarStringCache.Connect()
            .Bind(out _statusBarStrings)
            .Subscribe();

        StatusBarStringCache.AddRange(new[]
            {
                string.Format(Language.ModFileTreeViewModel_StatusBar_Files__0__1,
                    _totalNumFiles,
                    ByteSize.FromBytes(_totalSize).ToString()
                ),
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
                FileTreeNodeViewModel.CreateTreeSourceSizeColumn(),
                FileTreeNodeViewModel.CreateTreeSourceFileCountColumn(),
            },
        };
    }


}
