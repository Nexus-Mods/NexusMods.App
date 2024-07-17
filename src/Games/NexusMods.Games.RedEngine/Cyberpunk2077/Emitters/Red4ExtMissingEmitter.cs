using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Paths;

namespace NexusMods.Games.RedEngine.Cyberpunk2077.Emitters;

/// <summary>
/// Detects missing Red4Ext when a mod installs .dll files in the `red4ext/plugins` folder.
/// </summary>
public class Red4ExtMissingEmitter : APathBasedDependencyEmitterWithNexusDownload
{
    protected override string DependencyName => "Red4Ext";

    protected internal override IEnumerable<GamePath> DependencyPaths { get; } = new[]
    {
        // DLL hook file
        new GamePath(LocationId.Game, "bin/x64/winmm.dll"),
        // Actual mod loader
        new GamePath(LocationId.Game, "red4ext/RED4ext.dll"),
    };

    protected internal override GamePath[] DependantPaths { get; } = [new(LocationId.Game, "red4ext/plugins")];
    protected internal override Extension[] DependantExtensions { get; } = [new(".dll")];
    protected override GameDomain Domain  { get; } = Cyberpunk2077Game.StaticDomain;
    protected override ModId ModId { get; } = ModId.From(2380);
}
