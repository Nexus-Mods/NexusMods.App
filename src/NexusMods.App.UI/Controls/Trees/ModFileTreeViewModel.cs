using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using DynamicData;
using Humanizer.Bytes;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.App.UI.Controls.Trees.Files;
using NexusMods.App.UI.Helpers.TreeDataGrid;
using NexusMods.App.UI.Resources;

namespace NexusMods.App.UI.Controls.Trees;

/// <summary>
/// This is one implementation of the IFileTreeViewModel, which is used to display a tree of files in a mod.
/// </summary>
public class ModFileTreeViewModel : AViewModel<IFileTreeViewModel>, IFileTreeViewModel
{
    private readonly ILoadoutRegistry _registry;
    private readonly SourceCache<IFileTreeNodeViewModel, GamePath> _sourceCache;
    private ReadOnlyObservableCollection<IFileTreeNodeViewModel> _items;
    
    public ITreeDataGridSource<IFileTreeNodeViewModel> TreeSource { get; }
    
    public ModFileTreeViewModel(LoadoutId loadoutId, ModId modId, ILoadoutRegistry registry)
    {
        _registry = registry;
        _items = new ReadOnlyObservableCollection<IFileTreeNodeViewModel>([]);
        _sourceCache = new SourceCache<IFileTreeNodeViewModel, GamePath>(x => x.Key);
        
         var availableLocations = new HashSet<LocationId>();

        // Store GamePaths to dedupe the strings. No unsafe API in .NET to access the keys directly, but we need parent anyway, so it's ok.
        var folderToSize = new Dictionary<GamePath, (ulong size, uint numChildren, GamePath folder, GamePath parent, bool isLeaf)>();
        var mod = _registry.Get(loadoutId, modId)!; // <= suppressed because this throws on error, and we should always have valid mod if we made it here.
        var displayedItems = new List<IFileTreeNodeViewModel>();

        // TODO: Querying all of the files bottlenecks hard.
        // As this will be revised with EventSourcing, am not making a faster getter. 
        foreach (var file in mod.Files.Values)
        {
            // TODO: Check for IStoredFile, IToFile interfaces if we ever have more types of files that get put to disk.
            if (file is not StoredFile storedFile)
                continue;

            var folderName = storedFile.To.Parent;
            ref var item = ref CollectionsMarshal.GetValueRefOrNullRef(folderToSize, folderName);
            var exists = !Unsafe.IsNullRef(ref item);
            if (exists)
            {
                item.size += storedFile.Size.Value;
                item.numChildren++;
            }
            else 
            {
                folderToSize.Add(folderName, (storedFile.Size.Value, 1, folderName, folderName.Parent, true));
            }

            availableLocations.Add(storedFile.To.LocationId);
            displayedItems.Add(new FileTreeNodeViewModel(storedFile.To, folderName, true,
                    storedFile.Size.Value
                )
            );
        }

        // Make missing folders and update 'leaf' status.
        // It's possible that some folders only have subfolders, and not files, in which case they're missing from folderToSize.
        foreach (var existingItem in folderToSize.ToArray())
        {
            var parent = existingItem.Value.parent;
            while (parent.Path != "")
            {
                ref var item = ref CollectionsMarshal.GetValueRefOrNullRef(folderToSize, parent);
                var exists = !Unsafe.IsNullRef(ref item);
                var parentParent = parent.Parent;
                if (!exists)
                {
                    // We don't have a parent, so add a non-leaf node.
                    folderToSize.Add(parent, (0, parent, parentParent, false));
                    displayedItems.Add(new FileTreeNodeViewModel(parent, parent.Parent, false,
                            0
                        )
                    );
                }
                else
                {
                    item.isLeaf = false; // Mark the parent as a non-leaf node.
                }

                parent = parentParent;
            }
        }

        // Calculate folder sizes. Basically bubble up sizes of all leaf folders.
        foreach (var existingItem in folderToSize)
        {
            if (!existingItem.Value.isLeaf)
                continue;

            var parent = existingItem.Value.parent;
            while (parent.Path != "")
            {
                ref var item = ref CollectionsMarshal.GetValueRefOrNullRef(folderToSize, parent);
                Debug.Assert(!Unsafe.IsNullRef(ref item));
                item.size += existingItem.Value.size;
                parent = parent.Parent;
            }
        }

        // Now add up all of the folders.
        foreach (var item in folderToSize)
        {
            // But don't add the 'root' node.
            if (item.Value.folder.Path != "")
                displayedItems.Add(new FileTreeNodeViewModel(item.Value.folder, item.Value.parent, false,
                        item.Value.size
                    )
                );
        }

        _sourceCache.Clear();
        _sourceCache.AddOrUpdate(displayedItems);

        // Resolve folder locations.
        var namedLocations = new Dictionary<LocationId, string>();
        var loadout = _registry.Get(loadoutId);
        var register = loadout!.Installation.LocationsRegister;
        foreach (var location in availableLocations)
            namedLocations.Add(location, register[location].ToString());

        // Flatten them with DynamicData
        BindItems(_sourceCache, namedLocations, out _items
        );
        
        
        TreeSource = CreateTreeSource(_items);
        TreeSource.SortBy(TreeSource.Columns[0], ListSortDirection.Ascending);
    }
    
    /// <summary>
    ///     Binds all items in the given cache.
    ///     Root nodes are added for each locationId with children to show.
    /// </summary>
    internal static void BindItems(
        SourceCache<IFileTreeNodeViewModel, GamePath> cache,
        Dictionary<LocationId, string> locations,
        out ReadOnlyObservableCollection<IFileTreeNodeViewModel> result)
    {
        // Add AbsolutePath root nodes for each locationId with children to show
        foreach (var location in locations)
        {
            ulong totalSize = 0;
            foreach (var item in cache.Items)
            {
                if (item.Key.LocationId == location.Key && item.IsFile)
                    totalSize += item.FileSize;
            }

            cache.AddOrUpdate(new FileTreeNodeDesignViewModel(false, new GamePath(location.Key, ""), location.Value,
                    totalSize
                )
            );
        }

        cache.Connect()
            .TransformToTree(model => model.ParentKey)
            .Transform(node => node.Item.Initialize(node))
            .Bind(out result)
            .Subscribe(); // force evaluation
    }
    
    internal static HierarchicalTreeDataGridSource<IFileTreeNodeViewModel> CreateTreeSource(
        ReadOnlyObservableCollection<IFileTreeNodeViewModel> treeRoots)
    {
        return new HierarchicalTreeDataGridSource<IFileTreeNodeViewModel>(treeRoots)
        {
            Columns =
            {
                new HierarchicalExpanderColumn<IFileTreeNodeViewModel>(
                    new TemplateColumn<IFileTreeNodeViewModel>(
                        Language.Helpers_GenerateHeader_NAME,
                        "FileNameColumnTemplate",
                        width: new GridLength(1, GridUnitType.Star),
                        options: new TemplateColumnOptions<IFileTreeNodeViewModel>
                        {
                            // Compares if folder first, such that folders show first, then by file name.
                            CompareAscending = (x, y) =>
                            {
                                if (x == null || y == null) return 0;
                                var folderComparison = x.IsFile.CompareTo(y.IsFile);
                                return folderComparison != 0 ? folderComparison : string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
                            },

                            CompareDescending = (x, y) =>
                            {
                                if (x == null || y == null) return 0;
                                var folderComparison = x.IsFile.CompareTo(y.IsFile);
                                return folderComparison != 0 ? folderComparison : string.Compare(y.Name, x.Name, StringComparison.OrdinalIgnoreCase);
                            },
                        }
                    ),
                    node => node.Children,
                    null,
                    node => node.IsExpanded
                ),

                new TextColumn<IFileTreeNodeViewModel, string?>(
                    Language.Helpers_GenerateHeader_SIZE,
                    x => ByteSize.FromBytes(x.FileSize).ToString(),
                    options: new TextColumnOptions<IFileTreeNodeViewModel>
                    {
                        // Compares if folder first, such that folders show first, then by file name.
                        CompareAscending = (x, y) =>
                        {
                            if (x == null || y == null) return 0;
                            var folderComparison = x.IsFile.CompareTo(y.IsFile);
                            return folderComparison != 0 ? folderComparison : x.FileSize.CompareTo(y.FileSize);
                        },

                        CompareDescending = (x, y) =>
                        {
                            if (x == null || y == null) return 0;
                            var folderComparison = x.IsFile.CompareTo(y.IsFile);
                            return folderComparison != 0 ? folderComparison : y.FileSize.CompareTo(x.FileSize);
                        },
                    },
                    width: new GridLength(100)
                ),
            }
        };
    }
}
