using System.Collections.ObjectModel;
using DynamicData;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.App.UI.Controls.Trees.Files;
using NexusMods.Paths;

namespace NexusMods.App.UI.Controls.ModInfo.ViewModFiles;
using ModFileNode = TreeNodeVM<IFileTreeNodeViewModel, GamePath>;

public class ViewModFilesDesignViewModel : AViewModel<IViewModFilesViewModel>,
    IViewModFilesViewModel
{
    private ReadOnlyObservableCollection<ModFileNode> _items;

    public ReadOnlyObservableCollection<ModFileNode> Items => _items;

    public ViewModFilesDesignViewModel()
    {
        var cache = new SourceCache<IFileTreeNodeViewModel, GamePath>(x => x.FullPath);
        var locations = new Dictionary<LocationId, string>();
        const bool showMultipleRoots = false;
        const bool alwaysRootFolders = false; // always make 'saves', 'game' root nodes
        
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

#pragma warning disable CS0162 // Unreachable code detected
        if (showMultipleRoots)
        {
            // Add some saves
            locations.Add(LocationId.Saves, "SAVE");

            // Core save file
            SaveFile("Save 1 - Quicksave.ess"); 

            // SKSE co-save (if applicable)
            SaveFile("Save 1 - Quicksave.skse"); 

            // Configuration files
            SaveFile("SkyrimPrefs.ini");
            SaveFile("Skyrim.ini");
        }
#pragma warning restore CS0162 // Unreachable code detected

        // Assign
        ViewModFilesViewModel.BindItems(cache, locations, alwaysRootFolders, out _items);
    }

    public void Initialize(LoadoutId loadoutId, List<ModId> contextModIds)
    {
        //throw new NotImplementedException();
    }

    private static void CreateModFileNode(RelativePath filePath, LocationId locationId, SourceCache<IFileTreeNodeViewModel, GamePath> cache)
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
                cache.AddOrUpdate(new FileTreeNodeDesignViewModel(false, new GamePath(locationId, part)));
        }

        // Final part is the file
        cache.AddOrUpdate(new FileTreeNodeDesignViewModel(true, new GamePath(locationId, currentPath.Join(parts[index]))));
    }

}
