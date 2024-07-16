using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Paths;

namespace NexusMods.Games.RedEngine.Cyberpunk2077.Emitters;

/// <summary>
/// Diagnostic emitter for missing ArchiveXL when a mod installs .xl files in the game folder.
/// </summary>
public class ArchiveXLMissingEmitter : APathBasedDependencyEmitterWithNexusDownload
{
    protected override string DependencyName => "ArchiveXL";

    protected override IEnumerable<GamePath> DependencyPaths => new[]
    {
        // ArchiveXL DLL
        new GamePath(LocationId.Game, "red4ext/plugins/ArchiveXL/ArchiveXL.dll"),
    };

    protected override GamePath[] DependantPaths => [new GamePath(LocationId.Game, "")];
    
    protected override Extension[] DependantExtensions => [new(".xl")];
    protected override GameDomain Domain => Cyberpunk2077Game.StaticDomain;
    protected override ModId ModId => ModId.From(4198);
}
