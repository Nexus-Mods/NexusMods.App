using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using DynamicData;
using Examples.TreeDataGrid.SingleColumn.FileColumn;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Helpers.TreeDataGrid;
using NexusMods.Paths;

namespace Examples.TreeDataGrid.SingleColumn;

public class FileTreeDesignViewModel : AViewModel<IFileTreeViewModel>, IFileTreeViewModel
{
    private ReadOnlyObservableCollection<IFileColumnViewModel> _items;
    
    public ITreeDataGridSource<IFileColumnViewModel> TreeSource { get; }
    
    public FileTreeDesignViewModel()
    {
        _items = null!; // initialized in refresh
        RefreshData();
        TreeSource = CreateTreeSource(_items);
        TreeSource.SortBy(TreeSource.Columns[0], ListSortDirection.Ascending);
    }
    
    private void RefreshData()
    {
        var cache = new SourceCache<IFileColumnViewModel, GamePath>(x => x.Key);
        var locations = new Dictionary<LocationId, string>();

        // ReSharper disable once RedundantSuppressNullableWarningExpression
        void SaveFile(string filePath) => CreateModFileNode(filePath, LocationId.Saves, cache!);
        void GameFile(string filePath) => CreateModFileNode(filePath, LocationId.Game, cache);

        // Root Files
        locations.Add(LocationId.Game, "GAME");
        GameFile("bink2w64.dll");
        GameFile("High.ini");
        GameFile("installscript.vdf");
        GameFile("Low.ini");
        GameFile("Medium.ini");
        GameFile("Skyrim/SkyrimPrefs.ini");
        GameFile("Skyrim.ccc");
        GameFile("Skyrim_Default.ini");
        GameFile("SkyrimSE.exe");
        GameFile("SkyrimSELauncher.exe");
        GameFile("steam_api64.dll");
        GameFile("Ultra.ini");

        // Data Folder
        GameFile("Data/ccBGSSSE001-Fish.bsa");
        GameFile("Data/ccBGSSSE001-Fish.esm");
        GameFile("Data/ccBGSSSE025-AdvDSGS.bsa");
        GameFile("Data/ccBGSSSE025-AdvDSGS.esm");
        GameFile("Data/ccBGSSSE037-Curios.bsa");
        GameFile("Data/ccBGSSSE037-Curios.esl");
        GameFile("Data/ccQDRSSE001-SurvivalMode.bsa");
        GameFile("Data/ccQDRSSE001-SurvivalMode.esl");
        GameFile("Data/Dawnguard.esm");
        GameFile("Data/Dragonborn.esm");
        GameFile("Data/HearthFires.esm");
        GameFile("Data/MarketplaceTextures.bsa");
        GameFile("Data/_ResourcePack.bsa");
        GameFile("Data/_ResourcePack.esl");
        GameFile("Data/Skyrim - Animations.bsa");
        GameFile("Data/Skyrim.esm");
        GameFile("Data/Skyrim - Interface.bsa");
        GameFile("Data/Skyrim - Meshes0.bsa");
        GameFile("Data/Skyrim - Meshes1.bsa");
        GameFile("Data/Skyrim - Misc.bsa");
        GameFile("Data/Skyrim - Shaders.bsa");
        GameFile("Data/Skyrim - Sounds.bsa");
        GameFile("Data/Skyrim - Textures0.bsa");
        GameFile("Data/Skyrim - Textures1.bsa");
        GameFile("Data/Skyrim - Textures2.bsa");
        GameFile("Data/Skyrim - Textures3.bsa");
        GameFile("Data/Skyrim - Textures4.bsa");
        GameFile("Data/Skyrim - Textures5.bsa");
        GameFile("Data/Skyrim - Textures6.bsa");
        GameFile("Data/Skyrim - Textures7.bsa");
        GameFile("Data/Skyrim - Textures8.bsa");
        GameFile("Data/Skyrim - Voices_en0.bsa");
        GameFile("Data/Update.esm");

        // Video Folder
        GameFile("Video/BGS_Logo.bik");

        // Add some saves
        locations.Add(LocationId.Saves, "SAVE");
        SaveFile("Save 1 - Quicksave.ess"); // Core save file
        SaveFile("Save 1 - Quicksave.skse"); // SKSE co-save (if applicable)
        SaveFile("SkyrimPrefs.ini"); // Configuration file
        SaveFile("Skyrim.ini"); // Configuration file

        // Bind the Items out.
        // Add AbsolutePath root nodes for each locationId with children to show
        foreach (var location in locations)
            cache.AddOrUpdate(new FileColumnDesignViewModel(false, new GamePath(location.Key, ""), location.Value));

        // For 'root' nodes, we create a 'parent' at unknown location
        // in order to insert nodes which contain the roots, i.e. 'GAME', 'SAVE'.
        cache.Connect()
            .TransformToTree(model => model.Key.Path != "" 
                ? model.Key.Parent 
                : new GamePath(LocationId.Unknown, ""))
            .Transform(node => node.Item.Initialize(node))
            .Bind(out _items)
            .Subscribe(); // force evaluation
    }

    private static void CreateModFileNode(
        RelativePath filePath, 
        LocationId locationId, 
        SourceCache<IFileColumnViewModel, GamePath> cache)
    {
        // Build the path, creating directory nodes as needed
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
                cache.AddOrUpdate(new FileColumnDesignViewModel(false, new GamePath(locationId, part)));
        }

        // Final part is the file
        cache.AddOrUpdate(new FileColumnDesignViewModel(true, new GamePath(locationId, currentPath.Join(parts[index]))));
    }

    private static HierarchicalTreeDataGridSource<IFileColumnViewModel> CreateTreeSource(
        ReadOnlyObservableCollection<IFileColumnViewModel> treeRoots)
    {
        return new HierarchicalTreeDataGridSource<IFileColumnViewModel>(treeRoots)
        {
            Columns =
            {
                new HierarchicalExpanderColumn<IFileColumnViewModel>(
                    new TemplateColumn<IFileColumnViewModel>(
                        "NAME",
                        "FileNameColumnTemplate",
                        width: new GridLength(1, GridUnitType.Star),
                        options: new TemplateColumnOptions<IFileColumnViewModel>
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
                )
            }
        };
    }
}
