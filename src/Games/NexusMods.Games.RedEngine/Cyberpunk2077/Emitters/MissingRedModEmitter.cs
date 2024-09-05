using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Diagnostics.Values;
using NexusMods.Abstractions.Loadouts;
using static NexusMods.Games.RedEngine.Constants;
namespace NexusMods.Games.RedEngine.Cyberpunk2077.Emitters;

public partial class MissingRedModEmitter : ILoadoutDiagnosticEmitter
{
    public static readonly NamedLink RedmodLink = new("REDmod DLC", new Uri("https://www.cyberpunk.net/en/modding-support"));

    public async IAsyncEnumerable<Diagnostic> Diagnose(
        Loadout.ReadOnly loadout,
        CancellationToken cancellationToken)
    {
        var install = loadout.InstallationInstance.LocationsRegister;
        var redModPath = install.GetResolvedPath(RedModPath);        
        
        if (!redModPath.FileExists)
            yield return Diagnostics.CreateMissingRedModDependency(RedmodLink);
    }
}
