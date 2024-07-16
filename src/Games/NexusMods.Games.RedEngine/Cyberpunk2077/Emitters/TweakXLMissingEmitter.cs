using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Paths;

namespace NexusMods.Games.RedEngine.Cyberpunk2077.Emitters;

/// <summary>
/// Detects missing TweakXL when a mod installs .tweak, .yml or .yaml files in the `r6/tweaks` folder.
/// </summary>
public class TweakXLMissingEmitter : APathBasedDependencyEmitterWithNexusDownload
{
    protected override string DependencyName => "TweakXL";
    protected override IEnumerable<GamePath> DependencyPaths => new[]
    {
        // TweakXL DLL
        new GamePath(LocationId.Game, "red4ext/plugins/TweakXL/TweakXL.dll"),
    };
    protected override GamePath[] DependantPaths => [new(LocationId.Game, "r6/tweaks")];
    protected override Extension[] DependantExtensions => [new(".tweak"), new Extension(".yml"), new Extension(".yaml")];
    protected override GameDomain Domain => Cyberpunk2077Game.StaticDomain;
    protected override ModId ModId => ModId.From(4197);
}
