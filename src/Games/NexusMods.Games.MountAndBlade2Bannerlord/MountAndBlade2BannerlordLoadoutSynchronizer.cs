using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.Settings;
using NexusMods.Extensions.BCL;
using NexusMods.Games.MountAndBlade2Bannerlord.Models;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using static NexusMods.Games.MountAndBlade2Bannerlord.MountAndBlade2BannerlordConstants;

namespace NexusMods.Games.MountAndBlade2Bannerlord;

public class MountAndBlade2BannerlordLoadoutSynchronizer : ALoadoutSynchronizer
{
    // Paths to known locations
    private static GamePath GameGenegratedImGuiFile => new(LocationId.Game, "bin/Win64_Shipping_Client/imgui.ini");
    private static GamePath GameGeneratedSteamAppIdFile => new(LocationId.Game, "bin/Win64_Shipping_Client/steam_appid.txt");
    private static GamePath GameGenegratedImGuiFileXbox => new(LocationId.Game, "bin/Gaming.Desktop.x64_Shipping_Client/imgui.ini");
    private static GamePath GameGeneratedSteamAppIdFileXbox => new(LocationId.Game, "bin/Gaming.Desktop.x64_Shipping_Client/steam_appid.txt");
    
    // Base game mods that are part of the game distribution and which MUST be enabled
    private static GamePath ModuleNative => new(LocationId.Game, "Modules/Native");
    private static GamePath ModuleSandboxCore => new(LocationId.Game, "Modules/SandBoxCore");
    private static GamePath ModuleCustomBattle => new(LocationId.Game, "Modules/CustomBattle");
    private static GamePath ModuleSandbox => new(LocationId.Game, "Modules/SandBox");
    private static GamePath ModuleStoryMode => new(LocationId.Game, "Modules/StoryMode");
    private static GamePath ModuleBirthAndAgingOptions => new(LocationId.Game, "Modules/BirthAndDeath");
    private static GamePath ModuleMultiplayer => new(LocationId.Game, "Modules/BirthAndDeath");

    // Base game folders that are never modified
    private static GamePath BaseGameXmlSchemas => new(LocationId.Game, "XmlSchemas");
    private static GamePath BaseGameSounds => new(LocationId.Game, "Sounds");
    private static GamePath BaseGameShaders => new(LocationId.Game, "Shaders");
    private static GamePath BaseGameMusic => new(LocationId.Game, "music"); // yes it's lowercase
    private static GamePath BaseGameIcons => new(LocationId.Game, "Icons");
    private static GamePath BaseGameGui => new(LocationId.Game, "GUI");
    private static GamePath BaseGameDigitalCompanion => new(LocationId.Game, "DigitalCompanion");
    private static GamePath BaseGameData => new(LocationId.Game, "Data");
    private static GamePath BaseGameCrashUploader => new(LocationId.Game, "bin/CrashUploader.Publish");
    
    
    // Paths ignored for backup.
    private static readonly GamePath[] IgnoredBackupPaths =
    [
        GameGenegratedImGuiFile,
        GameGenegratedImGuiFileXbox,
        GameGeneratedSteamAppIdFile,
        GameGeneratedSteamAppIdFileXbox,
    ];
    
    // Folders ignored for backup.
    private static readonly GamePath[] IgnoredBackupFolders =
    [
        // Base game mods
        ModuleNative,
        ModuleSandboxCore,
        ModuleCustomBattle,
        ModuleSandbox,
        ModuleStoryMode,
        ModuleBirthAndAgingOptions,
        ModuleMultiplayer,
        
        // Base game folders that are never modified
        BaseGameXmlSchemas,
        BaseGameSounds,
        BaseGameShaders,
        BaseGameMusic,
        BaseGameIcons,
        BaseGameGui,
        BaseGameDigitalCompanion,
        BaseGameData,
        BaseGameCrashUploader,
    ];

    public MountAndBlade2BannerlordLoadoutSynchronizer(IServiceProvider provider) : base(provider)
    {
        var settingsManager = provider.GetRequiredService<ISettingsManager>();
        _settings = settingsManager.Get<MountAndBlade2BannerlordSettings>();
    }

    private readonly MountAndBlade2BannerlordSettings _settings;

    public override bool IsIgnoredBackupPath(GamePath path)
    {
        if (_settings.DoFullGameBackup) return false;
        return path.LocationId == LocationId.Game && IsIgnoredPathInner(path);
    }
    
    public override bool IsIgnoredPath(GamePath path) => !_settings.DoFullGameBackup && IsIgnoredPathInner(path);

    private static bool IsIgnoredPathInner(GamePath path)
    {
        // Note(sewer): No LINQ, game has a lot of files and a lot of things to ignore.
        // Ignore the standard module set if we're not doing a full game backup 
        foreach (var folder in IgnoredBackupFolders)
        {
            if (path.InFolder(folder))
                return true; 
        }
        
        // And ignore any runtime generated files that are not in game depot
        foreach (var ignoredPath in IgnoredBackupPaths)
        {
            if (path == ignoredPath)
                return true; 
        }

        return false;
    }

    protected override ValueTask MoveNewFilesToMods(Loadout.ReadOnly loadout, IEnumerable<AddedEntry> newFiles, ITransaction tx)
    {
        var modDirectoryNameToModel = new Dictionary<RelativePath, ModLoadoutItem.ReadOnly>();

        foreach (var newFile in newFiles)
        {
            if (!IsModFile(newFile.LoadoutItemWithTargetPath.TargetPath, out var modDirectoryName))
            {
                continue;
            }

            if (!modDirectoryNameToModel.TryGetValue(modDirectoryName, out var mod))
            {
                if (!TryGetMod(modDirectoryName, loadout, loadout.Db, out mod))
                {
                    continue;
                }

                modDirectoryNameToModel[modDirectoryName] = mod;
            }
            
            newFile.LoadoutItem.ParentId = mod.Id;
        }
        return ValueTask.CompletedTask;
    }

    private static bool TryGetMod(RelativePath modDirectoryName, Loadout.ReadOnly loadout, IDb db, out ModLoadoutItem.ReadOnly mod)
    {
        var manifestFilePath = new GamePath(LocationId.Game, ModsFolder.Join(modDirectoryName).Join(SubModuleFile));

        if (!LoadoutItemWithTargetPath.FindByTargetPath(db, manifestFilePath.ToGamePathParentTuple(loadout))
                .TryGetFirst(x => x.AsLoadoutItem().LoadoutId == loadout && x.Contains(ModuleInfoFileLoadoutFile.ModuleInfoFile), out var file))
        {
            mod = default(ModLoadoutItem.ReadOnly);
            return false;
        }

        mod = ModLoadoutItem.Load(db, file.AsLoadoutItem().Parent);
        return true;
    }

    private static bool IsModFile(GamePath gamePath, out RelativePath submoduleDirectoryName)
    {
        submoduleDirectoryName = RelativePath.Empty;
        if (gamePath.LocationId != LocationId.Game) return false;
        var path = gamePath.Path;

        if (!path.StartsWith(ModsFolder)) return false;
        path = path.DropFirst(numDirectories: 1);

        submoduleDirectoryName = path.TopParent;
        if (submoduleDirectoryName.Equals(RelativePath.Empty)) return false;

        return true;
    }
}
