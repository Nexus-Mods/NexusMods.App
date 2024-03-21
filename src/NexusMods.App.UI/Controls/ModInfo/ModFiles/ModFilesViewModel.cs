using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DynamicData;
using JetBrains.Annotations;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.App.UI.Controls.Trees.Files;
using NexusMods.App.UI.Helpers.TreeDataGrid;

namespace NexusMods.App.UI.Controls.ModInfo.ModFiles;

[UsedImplicitly]
public class ModFilesViewModel : AViewModel<IModFilesViewModel>, IModFilesViewModel
{
    private readonly ILoadoutRegistry _registry;
    private readonly SourceCache<IFileTreeNodeViewModel, GamePath> _sourceCache;
    private ReadOnlyObservableCollection<IFileTreeNodeViewModel> _items;

    public ReadOnlyObservableCollection<IFileTreeNodeViewModel> Items => _items;

    public ModFilesViewModel(ILoadoutRegistry registry, IFileStore fileStore)
    {
        _registry = registry;
        _items = new ReadOnlyObservableCollection<IFileTreeNodeViewModel>([]);
        _sourceCache = new SourceCache<IFileTreeNodeViewModel, GamePath>(x => x.Key);
    }

    public void Initialize(LoadoutId loadoutId, ModId modId)
    {
        var availableLocations = new HashSet<LocationId>();

        // Store GamePaths to dedupe the strings. No unsafe API in .NET to access the keys directly, but we need parent anyway, so it's ok.
        var folderToSize = new Dictionary<GamePath, (ulong size, GamePath folder, GamePath parent, bool isLeaf)>();
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
                item.size += storedFile.Size.Value;
            else
                folderToSize.Add(folderName, (storedFile.Size.Value, folderName, folderName.Parent, true));

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
}
