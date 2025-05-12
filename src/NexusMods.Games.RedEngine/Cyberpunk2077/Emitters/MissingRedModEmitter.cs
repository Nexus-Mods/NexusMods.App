using System.Runtime.CompilerServices;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Diagnostics.Values;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Paths;
using static NexusMods.Games.RedEngine.Constants;
namespace NexusMods.Games.RedEngine.Cyberpunk2077.Emitters;

public partial class MissingRedModEmitter : ILoadoutDiagnosticEmitter
{
    public static readonly NamedLink RedmodGenericLink = new("official website", new Uri("https://www.cyberpunk.net/en/modding-support"));
    public static readonly NamedLink RedmodSteamLink = new("Steam", new Uri("steam://store/2060310"));
    public static readonly NamedLink RedmodGOGLink = new("GOG Galaxy", new Uri("goggalaxy://openStoreUrl/embed.gog.com/game/cyberpunk_2077_redmod"));
    public static readonly NamedLink RedmodEGSLink = new("the Epic Games Store", new Uri("com.epicgames.launcher://store/p/cyberpunk-2077"));

    internal static bool HasRedMods(Loadout.ReadOnly loadout, out AbsolutePath redModInstallFolder, out int numRedModDirs)
    {
        redModInstallFolder = loadout.InstallationInstance.LocationsRegister.GetResolvedPath(RedModInstallFolder);

        if (!redModInstallFolder.DirectoryExists())
        {
            numRedModDirs = 0;
            return false;
        }

        var redModDirs = redModInstallFolder
            .EnumerateDirectories("*", false)
            .Where(x => x.EnumerateFiles(pattern: "*", recursive: false).Any());

        numRedModDirs = redModDirs.Count();
        return numRedModDirs > 0;
    }

    internal static bool HasRedModToolInstalled(Loadout.ReadOnly loadout, out AbsolutePath redModPath)
    {
        redModPath = loadout.InstallationInstance.LocationsRegister.GetResolvedPath(RedModPath);
        return redModPath.FileExists;
    }

    public async IAsyncEnumerable<Diagnostic> Diagnose(
        Loadout.ReadOnly loadout,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!HasRedMods(loadout, out var redModInstallFolder, out var numRedModDirs)) yield break;
        if (HasRedModToolInstalled(loadout, out var redModPath)) yield break;

        var store = loadout.InstallationInstance.Store;

        NamedLink link;
        if (store == GameStore.GOG)
            link = RedmodGOGLink;
        else if (store == GameStore.Steam)
            link = RedmodSteamLink;
        else if (store == GameStore.EGS)
            link = RedmodEGSLink;
        else
            link = RedmodGenericLink;

        yield return Diagnostics.CreateMissingRedModDependency(
            RedmodLink: link,
            GenericLink: RedmodGenericLink,
            ModCount: numRedModDirs,
            RedModFolder: redModInstallFolder.ToString(),
            RedModEXE: redModPath.ToString()
        );

        await Task.Yield();
    }
}
