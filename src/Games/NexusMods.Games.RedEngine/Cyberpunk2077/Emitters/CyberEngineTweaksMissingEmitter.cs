using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Paths;

namespace NexusMods.Games.RedEngine.Cyberpunk2077.Emitters;

/// <summary>
/// Detects missing Cyber Engine Tweaks when a mod installs .lua files in the `bin/x64/plugins/cyber_engine_tweaks` folder.
/// </summary>
public class CyberEngineTweaksMissingEmitter : APathBasedDependencyEmitterWithNexusDownload
{
    protected override string DependencyName => "Cyber Engine Tweaks";

    protected internal override IEnumerable<GamePath> DependencyPaths => new[]
    {
        // Hook file
        new GamePath(LocationId.Game, "bin/x64/version.dll"),
    };

    protected internal override GamePath[] DependantPaths => [new(LocationId.Game, "bin/x64/plugins/cyber_engine_tweaks")];
    protected internal override Extension[] DependantExtensions => [new(".lua")];
    protected override GameDomain Domain => Cyberpunk2077Game.StaticDomain;
    protected override ModId ModId => ModId.From(107);
}

