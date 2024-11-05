using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Diagnostics.Values;
using NexusMods.Abstractions.Loadouts;
namespace NexusMods.Games.Obsidian.FalloutNewVegas.Emitters;

public class MissingNVSEEmitter : ILoadoutDiagnosticEmitter
{
    public async IAsyncEnumerable<Diagnostic> Diagnose(Loadout.ReadOnly loadout, CancellationToken cancellationToken)
    {
        var install = loadout.InstallationInstance;
        var locations = install.LocationsRegister;
        var nvsePath = locations.GetResolvedPath(FalloutNewVegasConstants.NVSEPath);
        if (nvsePath.FileExists)
            yield break;

        yield return Diagnostics.CreateMissingNVSE(new NamedLink("xNVSE", new Uri("https://www.nexusmods.com/newvegas/mods/67883")));
    }
}
