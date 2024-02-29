using System.Collections.ObjectModel;
using DynamicData;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.App.UI.Controls.Trees.Files;
using ModFileNode = NexusMods.App.UI.TreeNodeVM<NexusMods.App.UI.Controls.Trees.Files.IFileTreeNodeViewModel, NexusMods.Abstractions.GameLocators.GamePath>;
namespace NexusMods.App.UI.Controls.ModInfo.ViewModFiles;

public class ViewModFilesViewModel : AViewModel<IViewModFilesViewModel>, IViewModFilesViewModel
{
    private readonly ILoadoutRegistry _registry;
    private readonly SourceCache<IFileTreeNodeViewModel, GamePath> _sourceCache;
    private ReadOnlyObservableCollection<ModFileNode> _items;

    public ReadOnlyObservableCollection<ModFileNode> Items => _items;

    private bool _hasMultipleRoots;
    public bool HasMultipleRoots => _hasMultipleRoots;

    private string? _primaryRootLocation;
    public string? PrimaryRootLocation => _primaryRootLocation;

    public ViewModFilesViewModel(ILoadoutRegistry registry)
    {
        _registry = registry;
        _items = new ReadOnlyObservableCollection<ModFileNode>([]);
        _sourceCache = new SourceCache<IFileTreeNodeViewModel, GamePath>(x => x.FullPath);
    }

    public void Initialize(LoadoutId loadoutId, List<ModId> contextModIds)
    {
        // Note: The code below only shows the 'raw' files, not as deployed in the case of multi-select.
        //       This is because games can use custom synchronizers, which means custom sort rules,
        //       so functionality such as 'get me sorted mods' can vary with game and need to instead
        //       be implemented in a loadout synchronizer.
        //
        //       In the UI, we will need some sort of warning that this does not represent the 'final' state.
        
        // Fetch all the files.
        var dict = new Dictionary<GamePath, ModFilePair>();
        var availableLocations = new HashSet<LocationId>();
        foreach (var modId in contextModIds)
        {
            var mod = _registry.Get(loadoutId, modId);
            if (mod == null)
                continue;

            foreach (var (_, file) in mod.Files)
            {
                if (file is not IToFile toFile)
                    continue;

                dict[toFile.To] = new ModFilePair { Mod = mod, File = file };
                availableLocations.Add(toFile.To.LocationId);
            }
        }

        // Add them to the cache.
        var roots = FlattenedLoadout.Create(dict)
            .GetAllDescendents()
            .Select(x => (IFileTreeNodeViewModel)new FileTreeNodeViewModel<ModFilePair>(x));
 
        // Create folder nodes for all of the roots
        _sourceCache.Clear();
        _sourceCache.AddOrUpdate(roots);
        
        // Resolve folder locations.
        var namedLocations = new Dictionary<LocationId, string>();
        var loadout = _registry.Get(loadoutId);
        var register = loadout!.Installation.LocationsRegister;
        foreach (var location in availableLocations)
            namedLocations.Add(location, register[location].ToString());
        
        // Flatten them with DynamicData
        BindItems(_sourceCache, namedLocations, false, out _items, out _hasMultipleRoots, out _primaryRootLocation);
    }
    
    /// <summary>
    ///     Binds all items in the given cache.
    ///     If the items have multiple roots (LocationIds), separate nodes are made for them.
    /// </summary>
    internal static void BindItems(
        SourceCache<IFileTreeNodeViewModel, GamePath> cache, 
        Dictionary<LocationId, string> locations, 
        bool alwaysRoot, 
        out ReadOnlyObservableCollection<ModFileNode> result, 
        out bool hasMultipleRoots,
        out string? primaryRootLocation)
    {
        // AlwaysRoot is left as a parameter because it may be a user preference in settings in the future.
        // If there's more than 1 location, create dummy nodes.
        hasMultipleRoots = (alwaysRoot || locations.Count > 1); 
        if (hasMultipleRoots)
        {
            foreach (var location in locations)
                cache.AddOrUpdate(new FileTreeNodeDesignViewModel(false, new GamePath(location.Key, ""), location.Value));

            hasMultipleRoots = true;
            primaryRootLocation = null;
        }
        else
        {
            primaryRootLocation = locations.First().Value;
        }
        
        cache.Connect()
            .TransformToTree(model => model.ParentPath)
            .Transform(node => new ModFileNode(node))
            .Bind(out result)
            .Subscribe(); // force evaluation
    }
}
