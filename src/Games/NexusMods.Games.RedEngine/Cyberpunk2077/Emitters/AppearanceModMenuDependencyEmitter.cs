using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Paths;

namespace NexusMods.Games.RedEngine.Cyberpunk2077.Emitters;

/// <summary>
/// Appearance Mod Menu is a mod based on CET that adds an in-game menu for changing appearances of NPCs and the player. It has a lua registration
/// script, and a collabs folder that contains additional scripts that other mods can use to extend the functionality.
/// </summary>
public class AppearanceModMenuDependencyEmitter : APathBasedDependencyEmitterWithNexusDownload
{
    protected override string DependencyName => "Appearance Menu Mod (AMM)";

    protected internal override IEnumerable<GamePath> DependencyPaths { get; } =
    [
        new GamePath(LocationId.Game, "bin/x64/plugins/cyber_engine_tweaks/mods/AppearanceMenuMod/init.lua"),
    ];

    protected internal override GamePath[] DependantPaths { get; } = [new GamePath(LocationId.Game, "bin/x64/plugins/cyber_engine_tweaks/mods/AppearanceMenuMod/Collabs")];
    
    protected internal override Extension[] DependantExtensions { get; } = [new(".lua")];
    protected override GameDomain Domain => Cyberpunk2077Game.StaticDomain;
    protected override ModId ModId { get; } = ModId.From(790);
}
