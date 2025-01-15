using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using DynamicData;
using Examples.TreeDataGrid.Basic.ViewModel;
using JetBrains.Annotations;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Helpers.TreeDataGrid;
using NexusMods.Paths;

namespace Examples.TreeDataGrid.Basic;

[UsedImplicitly] // Designer
public class FileTreeDesignViewModel : AViewModel<IFileTreeViewModel>, IFileTreeViewModel
{
    public ITreeDataGridSource<IFileViewModel> TreeSource { get; }
    
    public FileTreeDesignViewModel()
    {
        var cache = new SourceCache<IFileViewModel, GamePath>(x => x.Key);

        // Create some test data.
        // ReSharper disable once RedundantSuppressNullableWarningExpression
        void GameFile(string filePath) => CreateModFileNode(filePath, cache);

        // Root Files
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

        // Construct DynamicData Tree
        cache.Connect()
            .TransformToTree(model => model.Key.Parent)
            .Transform(node => node.Item.Initialize(node))
            .Bind(out var items)
            .Subscribe(); // force evaluation
        
        // Create the Columns for the Tree
        TreeSource = CreateTreeSource(items);
    }

    private static HierarchicalTreeDataGridSource<IFileViewModel> CreateTreeSource(
        ReadOnlyObservableCollection<IFileViewModel> treeRoots)
    {
        return new HierarchicalTreeDataGridSource<IFileViewModel>(treeRoots)
        {
            Columns =
            {
                new HierarchicalExpanderColumn<IFileViewModel>(
                    new TextColumn<IFileViewModel,string>("Name", x => x.Name, width: new GridLength(1, GridUnitType.Star)),
                        node => node.Children,
                        null,
                        node => node.IsExpanded
                    ),
            },
        };
    }
    
    private static void CreateModFileNode(
        RelativePath filePath, 
        SourceCache<IFileViewModel, GamePath> cache)
    {
        // Build the path, creating directory nodes as needed
        var currentPath = new RelativePath();
        var parts = filePath.GetParts();
        var index = 0;
        for (; index < parts.Length - 1; index++)
        {
            var part = filePath.Parts.ToArray()[index];
            currentPath = currentPath.Join(part);
            var curGamePath = new GamePath(LocationId.Game, currentPath);

            // Check if the directory already exists
            if (!cache.Lookup(curGamePath).HasValue)
                cache.AddOrUpdate(new FileDesignViewModel(new GamePath(LocationId.Game, part)));
        }

        // Final part is the file
        cache.AddOrUpdate(new FileDesignViewModel(new GamePath(LocationId.Game, currentPath.Join(parts[index]))));
    }
}
