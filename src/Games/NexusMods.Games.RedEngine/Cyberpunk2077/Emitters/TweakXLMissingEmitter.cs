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
    protected internal override IEnumerable<GamePath> DependencyPaths { get; } = new[]
    {
        // TweakXL DLL
        new GamePath(LocationId.Game, "red4ext/plugins/TweakXL/TweakXL.dll"),
    };
    protected internal override GamePath[] DependantPaths { get; } = [new(LocationId.Game, "r6/tweaks")];
    protected internal override Extension[] DependantExtensions { get; } = [new(".tweak"), new Extension(".yml"), new Extension(".yaml")];
    protected override GameDomain Domain { get; } = Cyberpunk2077Game.StaticDomain;
    protected override ModId ModId { get; } = ModId.From(4197);
}
