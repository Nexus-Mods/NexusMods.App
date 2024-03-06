using System.Collections.ObjectModel;
using DynamicData;
using JetBrains.Annotations;
using NexusMods.Abstractions.GameLocators;
using NexusMods.App.UI.Controls.Trees.Files;
using NexusMods.Paths;

namespace NexusMods.App.UI.Controls.ModInfo.ModFiles;
using ModFileNode = TreeNodeVM<IFileTreeNodeViewModel, GamePath>;

public class ModFilesViewModFilesDesignModFilesViewModel : AViewModel<IModFilesViewModel>,
    IModFilesViewModel
{
    private ReadOnlyObservableCollection<ModFileNode> _items;
    private int _rootCount;

    public int RootCount => _rootCount;
    
    public ReadOnlyObservableCollection<ModFileNode> Items => _items;

    private string? _primaryRootLocation;
    public string? PrimaryRootLocation => _primaryRootLocation;

    private bool _showMultipleRoots;
    private bool _alwaysRootFolders;
    
    /// <summary>
    ///     Adds a 'saves' folder.
    /// </summary>
    public bool ShowMultipleRoots
    {
        [UsedImplicitly]
        get => _showMultipleRoots;
        set
        {
            _showMultipleRoots = value;
            RefreshData(_showMultipleRoots, _alwaysRootFolders);
        }
    }
    
    /// <summary>
    ///     Always make 'saves', 'game' root nodes.
    /// </summary>
    public bool AlwaysRootFolders
    {
        [UsedImplicitly]
        get => _alwaysRootFolders;
        set
        {
            _alwaysRootFolders = value;
            RefreshData(_showMultipleRoots, _alwaysRootFolders);
        }
    }
    
    public ModFilesViewModFilesDesignModFilesViewModel()
    {
        _items = null!; // initialized in refresh
        RefreshData(false, false);
    }

    private void RefreshData(bool showMultipleRoots, bool alwaysRootFolders)
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

        if (showMultipleRoots)
        {
            // Add some saves
            locations.Add(LocationId.Saves, "SAVE");

            // Core save file
            SaveFile("Save 1 - Quicksave.ess", 4000000);

            // SKSE co-save (if applicable)
            SaveFile("Save 1 - Quicksave.skse", 800000);

            // Configuration files
            SaveFile("SkyrimPrefs.ini", 15000);
            SaveFile("Skyrim.ini", 10000);
        }

        // Assign
        ModFilesViewModel.BindItems(cache, locations, alwaysRootFolders, out _items, out _rootCount, out _primaryRootLocation);
    }

    private static void CreateModFileNode(RelativePath filePath, LocationId locationId, SourceCache<IFileTreeNodeViewModel, GamePath> cache, ulong fileSize)
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
        cache.AddOrUpdate(new FileTreeNodeDesignViewModel(true, new GamePath(locationId, currentPath.Join(parts[index])), fileSize));
    }
}
