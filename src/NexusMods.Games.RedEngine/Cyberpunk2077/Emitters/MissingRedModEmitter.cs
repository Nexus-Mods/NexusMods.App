using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Diagnostics.Values;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using static NexusMods.Games.RedEngine.Constants;
namespace NexusMods.Games.RedEngine.Cyberpunk2077.Emitters;

public partial class MissingRedModEmitter : ILoadoutDiagnosticEmitter
{
    public static readonly NamedLink RedmodGenericLink = new("REDmod DLC", new Uri("https://www.cyberpunk.net/en/modding-support"));
    public static readonly NamedLink RedmodSteamLink = new("REDmod DLC", new Uri("steam://store/2060310"));
    public static readonly NamedLink RedmodGOGLink = new("REDmod DLC", new Uri("goggalaxy://openStoreUrl/embed.gog.com/game/cyberpunk_2077_redmod"));
    public static readonly NamedLink RedmodEGSLink = new("REDmod DLC", new Uri("com.epicgames.launcher://store/p/cyberpunk-2077"));

    public async IAsyncEnumerable<Diagnostic> Diagnose(
        Loadout.ReadOnly loadout,
        CancellationToken cancellationToken)
    {
        var install = loadout.InstallationInstance;
        var locations = install.LocationsRegister;
        var redModPath = locations.GetResolvedPath(RedModPath);

        if (redModPath.FileExists)
            yield break;

        NamedLink link;
        if (install.Store == GameStore.GOG)
            link = RedmodGOGLink;
        else if (install.Store == GameStore.Steam)
            link = RedmodSteamLink;
        else if (install.Store == GameStore.EGS)
            link = RedmodEGSLink;
        else
            link = RedmodGenericLink;

        yield return Diagnostics.CreateMissingRedModDependency(link);
    }
}
