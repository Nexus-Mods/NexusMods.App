using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using DynamicData;
using Humanizer.Bytes;
using NexusMods.Abstractions.GameLocators;
using NexusMods.App.UI;
using NexusMods.App.UI.Controls.Trees.Files;
using NexusMods.App.UI.Helpers.TreeDataGrid;
using NexusMods.App.UI.Resources;
using NexusMods.Paths;

namespace Examples.TreeDataGrid.SingleColumn;

public class FileTreeDesignViewModel : AViewModel<IFileTreeViewModel>, IFileTreeViewModel
{
    private ReadOnlyObservableCollection<IFileTreeNodeViewModel> _items;
    
    public ITreeDataGridSource<IFileTreeNodeViewModel> TreeSource { get; }
    
    public FileTreeDesignViewModel()
    {
        _items = null!; // initialized in refresh
        RefreshData();
        TreeSource = CreateTreeSource(_items);
        TreeSource.SortBy(TreeSource.Columns[0], ListSortDirection.Ascending);
    }
    
    private void RefreshData()
    {
        var cache = new SourceCache<IFileTreeNodeViewModel, GamePath>(x => x.Key);
        var locations = new Dictionary<LocationId, string>();

        // ReSharper disable once RedundantSuppressNullableWarningExpression
        void SaveFile(string filePath, ulong fileSize) => CreateModFileNode(filePath, LocationId.Saves, cache!, fileSize);
        void GameFile(string filePath, ulong fileSize) => CreateModFileNode(filePath, LocationId.Game, cache, fileSize);

        // Root Files
        locations.Add(LocationId.Game, "GAME");
        GameFile("bink2w64.dll", 391360);
        GameFile("High.ini", 906);
        GameFile("installscript.vdf", 648);
        GameFile("Low.ini", 898);
        GameFile("Medium.ini", 898);
        GameFile("Skyrim/SkyrimPrefs.ini", 3498);
        GameFile("Skyrim.ccc", 2035);
        GameFile("Skyrim_Default.ini", 1859);
        GameFile("SkyrimSE.exe", 37157144);
        GameFile("SkyrimSELauncher.exe", 4713472);
        GameFile("steam_api64.dll", 298384);
        GameFile("Ultra.ini", 911);

        // Data Folder
        GameFile("Data/ccBGSSSE001-Fish.bsa", 377675522);
        GameFile("Data/ccBGSSSE001-Fish.esm", 1425176);
        GameFile("Data/ccBGSSSE025-AdvDSGS.bsa", 1092876237);
        GameFile("Data/ccBGSSSE025-AdvDSGS.esm", 812873);
        GameFile("Data/ccBGSSSE037-Curios.bsa", 111740475);
        GameFile("Data/ccBGSSSE037-Curios.esl", 37476);
        GameFile("Data/ccQDRSSE001-SurvivalMode.bsa", 12835601);
        GameFile("Data/ccQDRSSE001-SurvivalMode.esl", 240724);
        GameFile("Data/Dawnguard.esm", 25885111);
        GameFile("Data/Dragonborn.esm", 64663863);
        GameFile("Data/HearthFires.esm", 3977420);
        GameFile("Data/MarketplaceTextures.bsa", 2938167);
        GameFile("Data/_ResourcePack.bsa", 916509890);
        GameFile("Data/_ResourcePack.esl", 78418);
        GameFile("Data/Skyrim - Animations.bsa", 65155583);
        GameFile("Data/Skyrim.esm", 249753412);
        GameFile("Data/Skyrim - Interface.bsa", 105799354);
        GameFile("Data/Skyrim - Meshes0.bsa", 1153126177);
        GameFile("Data/Skyrim - Meshes1.bsa", 378762772);
        GameFile("Data/Skyrim - Misc.bsa", 17713449);
        GameFile("Data/Skyrim - Shaders.bsa", 67308970);
        GameFile("Data/Skyrim - Sounds.bsa", 1538656059);
        GameFile("Data/Skyrim - Textures0.bsa", 652490581);
        GameFile("Data/Skyrim - Textures1.bsa", 1511492648);
        GameFile("Data/Skyrim - Textures2.bsa", 1345109830);
        GameFile("Data/Skyrim - Textures3.bsa", 1406304717);
        GameFile("Data/Skyrim - Textures4.bsa", 1265325033);
        GameFile("Data/Skyrim - Textures5.bsa", 813639992);
        GameFile("Data/Skyrim - Textures6.bsa", 97210200);
        GameFile("Data/Skyrim - Textures7.bsa", 677006639);
        GameFile("Data/Skyrim - Textures8.bsa", 255927303);
        GameFile("Data/Skyrim - Voices_en0.bsa", 1807969854);
        GameFile("Data/Update.esm", 18874041);

        // Video Folder
        GameFile("Video/BGS_Logo.bik", 13835808);

        // Add some saves
        locations.Add(LocationId.Saves, "SAVE");
        SaveFile("Save 1 - Quicksave.ess", 4000000); // Core save file
        SaveFile("Save 1 - Quicksave.skse", 800000); // SKSE co-save (if applicable)
        SaveFile("SkyrimPrefs.ini", 15000); // Configuration file
        SaveFile("Skyrim.ini", 10000); // Configuration file

        // Assign
        BindItems(cache, locations, out _items);
    }

    private static void CreateModFileNode(
        RelativePath filePath, 
        LocationId locationId, 
        SourceCache<IFileTreeNodeViewModel, GamePath> cache, 
        ulong fileSize)
    {
        // Build the path, creating directories as needed
        var currentPath = new RelativePath();
        var parts = filePath.GetParts();
        var index = 0;
        for (; index < parts.Length - 1; index++)
        {
            var part = filePath.Parts.ToArray()[index];
            currentPath = currentPath.Join(part);
            var curGamePath = new GamePath(locationId, currentPath);

            // Check if the directory already exists
            if (!cache.Lookup(curGamePath).HasValue)
                cache.AddOrUpdate(new FileTreeNodeDesignViewModel(false, new GamePath(locationId, part), fileSize));
        }

        // Final part is the file
        cache.AddOrUpdate(new FileTreeNodeDesignViewModel(
            true, 
            new GamePath(locationId, currentPath.Join(parts[index])), 
            fileSize));
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
