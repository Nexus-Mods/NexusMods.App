using System.Text.RegularExpressions;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Paths;

namespace NexusMods.Games.RedEngine.Cyberpunk2077.Emitters;

public static partial class PatternDefinitions
{
    public static readonly Pattern[] Definitions =
    [
        new Pattern
        {
            DependencyName = "Codeware",
            DependencyPaths =
            [
                new GamePath(LocationId.Game, "red4ext/plugins/CodeWare/CodeWare.dll"),
            ],
            ModId = ModId.From(7780),
            DependantSearchPatterns =
            [
                new DependantSearchPattern
                {
                    Path = new GamePath(LocationId.Game, ""),
                    Extension = new Extension(".reds"),
                    Regex = ExtendsScriptableService(),
                },
                new DependantSearchPattern
                {
                    Path = new GamePath(LocationId.Game, ""),
                    Extension = new Extension(".lua"),
                    Regex = CodewareRegex(),
                },
            ],
            Explanation = """
Codeware is an extension to Cyberpunk that adds several utility classes and methods to the game. We detect the use of this library by looking for uses of 'ScriptableService' in
'.reds' files, and the global 'Codeware' in '.lua' files.                                                    
""",
        },
        new Pattern
        {
            DependencyName = "Virtual Atelier",
            DependencyPaths =
            [
                new GamePath(LocationId.Game, "archive/pc/mod/VirtualAtelier.archive"),
                new GamePath(LocationId.Game, "archive/pc/mod/VirtualAtelier.archive.xl"),
                new GamePath(LocationId.Game, "r6/scripts/virtual-atelier-full/core/Events.reds"),
            ],
            ModId = ModId.From(2987),
            DependantSearchPatterns =
            [
                new DependantSearchPattern
                {
                    Path = new GamePath(LocationId.Game, "r6/scripts"),
                    Extension = new Extension(".reds"),
                    Regex = VirtualShopRegistrationMatcher(), 
                },
            ],
            Explanation = """
Virtual Atelier is a mod that adds a virtual shop to the game. It provides a RedScript hook that other mods can use to register their own items 
with the shop. We scan `.reds` files for this hook to detect the use of this mod.
""",
        },
        new Pattern
        {
            DependencyName = "Appearance Menu Mod (AMM)",
            DependencyPaths =
            [
                new GamePath(LocationId.Game, "bin/x64/plugins/cyber_engine_tweaks/mods/AppearanceMenuMod/init.lua"),
            ],
            ModId = ModId.From(790),
            DependantSearchPatterns = [
                new DependantSearchPattern
                {
                    Path = new GamePath(LocationId.Game, "bin/x64/plugins/cyber_engine_tweaks/mods/AppearanceMenuMod/Collabs"),
                    Extension = new Extension(".lua"),
                },
            ],
            Explanation = """
Appearance Menu Mod is a mod based on CET that adds an in-game menu for changing appearances of NPCs and the player. It has a lua registration
script, and a `Collabs` folder that allows other mods to extend its functionality. We look for `.lua` files in the `Collabs` folder to detect the use of this mod.                          
""",
        },
        new Pattern
        {
            DependencyName = "Red4Ext",
            DependencyPaths =
            [
                new GamePath(LocationId.Game, "bin/x64/winmm.dll"),
                new GamePath(LocationId.Game, "red4ext/RED4ext.dll"),
            ],
            ModId = ModId.From(2380),
            DependantSearchPatterns = 
            [
                new DependantSearchPattern {
                    Path = new GamePath(LocationId.Game, "red4ext/plugins"),
                    Extension = new Extension(".dll"),
                },
                new DependantSearchPattern
                {
                    Path = new GamePath(LocationId.Game, "r6"),
                    Extension = new Extension(".reds"),
                },
            ],
            Explanation = """
Red4Ext is a mod loader for Cyberpunk that allows other mods to hook into the game. We detect the use of this mod by looking for `.dll` files in the `red4ext/plugins` folder, and for `.reds` files in the `r6` folder. We 
detect that this mod is installed by looking for the presence of `winmm.dll` in the game folder, and `RED4ext.dll` in the `red4ext` folder.                          
""",
        },
        new Pattern
        {
            DependencyName = "TweakXL",
            DependencyPaths =
            [
                new GamePath(LocationId.Game, "red4ext/plugins/TweakXL/TweakXL.dll"),
            ],
            ModId = ModId.From(4197),
            DependantSearchPatterns = [
                new DependantSearchPattern
                {
                    Path = new GamePath(LocationId.Game, "r6/tweaks"),
                    Extension = new Extension(".tweak"),
                },
                new DependantSearchPattern
                {
                    Path = new GamePath(LocationId.Game, "r6/tweaks"),
                    Extension = new Extension(".yml"),
                },
                new DependantSearchPattern
                {
                    Path = new GamePath(LocationId.Game, "r6/tweaks"),
                    Extension = new Extension(".yaml"),
                },
            ],
            Explanation = """
TweakXL is a mod that allows other mods to add tweaks to the game's internal data structures. We detect the use of this mod by looking for `.tweak`, `.yml`, and `.yaml` files in the `r6/tweaks` folder.
And we detect that this mod is installed by looking for `TweakXL.dll` in the `red4ext/plugins` folder.                          
""",
        },
        new Pattern
        {
            DependencyName = "ArchiveXL",
            DependencyPaths =
            [
                new GamePath(LocationId.Game, "red4ext/plugins/ArchiveXL/ArchiveXL.dll"),
            ],
            ModId = ModId.From(4198),
            DependantSearchPatterns = [
                new DependantSearchPattern
                {
                    Path = new GamePath(LocationId.Game, "mods"),
                    Extension = new Extension(".xl"),
                },
                new DependantSearchPattern
                {
                    Path = new GamePath(LocationId.Game, "archive"),
                    Extension = new Extension(".xl"),
                }
            ],
            Explanation = """
ArchiveXL is a mod that allows other mods to load custom resources without touching the original game files. We detect the use of this mod by looking for `.xl` files in the game folder, and we 
detect that this mod is installed by looking for `ArchiveXL.dll` in the `red4ext/plugins` folder.                          
""",
        },
        new Pattern
        {
            DependencyName = "Cyber Engine Tweaks (CET)",
            DependencyPaths =
            [
                new GamePath(LocationId.Game, "bin/x64/version.dll"),
            ],
            ModId = ModId.From(107),
            DependantSearchPatterns = [
                new DependantSearchPattern
                {
                    Path = new GamePath(LocationId.Game, "bin/x64/plugins/cyber_engine_tweaks"),
                    Extension = new Extension(".lua"),
                },
            ],
            Explanation = """
Cyber Engine Tweaks (also known as CET) is a mod that allows other mods to hook into the game. We detect the use of this mod by looking for `.lua` files
in the `bin/x64/plugins/cyber_engine_tweaks` folder, and we detect that this mod is installed by looking for `version.dll` in the game folder.                          
""",
        },
        new Pattern
        {
            DependencyName = "Appearance Change Unlocker",
            DependencyPaths =
            [
                new GamePath(LocationId.Game, "bin/x64/plugins/cyber_engine_tweaks/mods/AppearanceChangeUnlocker/init.lua"),
            ],
            ModId = ModId.From(3850),
            DependantSearchPatterns = [
                new DependantSearchPattern
                {
                    Path = new GamePath(LocationId.Game, "bin/x64/plugins/cyber_engine_tweaks/mods/AppearanceChangeUnlocker/character-preset"),
                    Extension = new Extension(".preset"),
                },
            ],
            Explanation = """
Appearance Change Unlocker is a mod that allows you to load and save character presets during character creation. 
We detect the use of this mod by looking for `.preset` files in the `character-preset` folder, and we 
detect that this mod is installed by looking for `init.lua` in the `bin/x64/plugins/cyber_engine_tweaks/mods/AppearanceChangeUnlocker` folder.                          
""",
        },
    ];

#region GeneratedRegexes
    
    [GeneratedRegex(@"extends\s+ScriptableService\s+{")]
    private static partial Regex ExtendsScriptableService();
    
    [GeneratedRegex(@"\s+Codeware\s+")]
    private static partial Regex CodewareRegex();
    
    [GeneratedRegex("ref<VirtualShopRegistration>")]
    private static partial Regex VirtualShopRegistrationMatcher();
    
#endregion

}
