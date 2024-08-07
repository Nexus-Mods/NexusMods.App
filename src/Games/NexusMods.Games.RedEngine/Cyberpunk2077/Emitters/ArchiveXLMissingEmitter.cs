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

    protected internal override IEnumerable<GamePath> DependencyPaths { get; } = new[]
    {
        // ArchiveXL DLL
        new GamePath(LocationId.Game, "red4ext/plugins/ArchiveXL/ArchiveXL.dll"),
    };

    protected internal override GamePath[] DependantPaths { get; } = [new GamePath(LocationId.Game, "")];
    
    protected internal override Extension[] DependantExtensions { get; } = [new(".xl")];
    protected override GameDomain Domain { get; } = Cyberpunk2077Game.StaticDomain;
    protected override ModId ModId { get; } = ModId.From(4198);
}
