using System.Runtime.CompilerServices;
using DynamicData.Kernel;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Diagnostics.Values;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.Abstractions.Resources;
using NexusMods.Abstractions.Telemetry;
using NexusMods.Extensions.BCL;
using NexusMods.Games.StardewValley.Models;
using StardewModdingAPI;
using StardewModdingAPI.Toolkit;
using StardewModdingAPI.Toolkit.Serialization.Models;

namespace NexusMods.Games.StardewValley.Emitters;

internal static class Helpers
{
    public static readonly NamedLink NexusModsLink = new("Nexus Mods", NexusModsUrlBuilder.CreateGenericUri("https://nexusmods.com/stardewvalley"));
    public static readonly NamedLink SMAPILink = new("Nexus Mods", NexusModsUrlBuilder.CreateDiagnosticUri(StardewValley.DomainStatic.Value, "2400"));

    public static ISemanticVersion GetGameVersion(Loadout.ReadOnly loadout)
    {
        var game = (loadout.InstallationInstance.Game as AGame)!;
        var localVersion = game.GetLocalVersion(loadout.Installation).Convert(static v => v.ToString());
        var rawVersion = localVersion.ValueOr(() => loadout.GameVersion);

#if DEBUG
        // NOTE(erri120): dumb hack for tests
        var index = rawVersion.IndexOf(".stubbed", StringComparison.OrdinalIgnoreCase);
        if (index != -1)
        {
            rawVersion = rawVersion.AsSpan()[..index].ToString();
        }
#endif

        var gameVersion = new SemanticVersion(rawVersion);
        return gameVersion;
    }

    public static bool TryGetSMAPI(Loadout.ReadOnly loadout, out SMAPILoadoutItem.ReadOnly smapi)
    {
        var foundSMAPI = loadout.Items
            .OfTypeLoadoutItemGroup()
            .OfTypeSMAPILoadoutItem()
            .TryGetFirst(x => x.AsLoadoutItemGroup().AsLoadoutItem().IsEnabled(), out smapi);

        return foundSMAPI;
    }

    public static async IAsyncEnumerable<ValueTuple<SMAPIModLoadoutItem.ReadOnly, Manifest>> GetAllManifestsAsync(
        ILogger logger,
        Loadout.ReadOnly loadout,
        IResourceLoader<SMAPIModLoadoutItem.ReadOnly, Manifest> pipeline,
        bool onlyEnabledMods,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var asyncEnumerable = loadout.Items
            .OfTypeLoadoutItemGroup()
            .OfTypeSMAPIModLoadoutItem()
            .Where(x => !onlyEnabledMods || x.AsLoadoutItemGroup().AsLoadoutItem().IsEnabled())
            .ToAsyncEnumerable()
            .ConfigureAwait(continueOnCapturedContext: false)
            .WithCancellation(cancellationToken);

        await using var enumerator = asyncEnumerable.GetAsyncEnumerator();
        while (await enumerator.MoveNextAsync())
        {
            var smapiMod = enumerator.Current;

            Resource<Manifest> resource;

            try
            {
                resource = await pipeline.LoadResourceAsync(smapiMod, cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Exception while getting manifest for `{Name}`", smapiMod.AsLoadoutItemGroup().AsLoadoutItem().Name);
                continue;
            }

            yield return (smapiMod, resource.Data);
        }
    }

    public static LogFilePathWithEditTime? GetLatestSMAPILogFile(ILogger _logger)
    {
        // Check if the SMAPI logs folder exists (may require the game to be run at least once with SMAPI)
        if (!Directory.Exists(Constants.SMAPILogsFolder))
        {
            return null;
        }

        // Get all files in the folder
        string[] files = Directory.GetFiles(Constants.SMAPILogsFolder);

        // Find any SMAPI log files and sort by creation time.
        var logFiles = files
            .Where(file => file.EndsWith(Constants.SMAPILogFileName) || file.EndsWith(Constants.SMAPIErrorFileName))
            .Select(file => new LogFilePathWithEditTime(file, File.GetLastWriteTime(file)))
            .OrderBy(file => file.EditTime)
            .ToList();

        if (logFiles.Any())
        {
            // Return the newest log file.
            return logFiles.Last();

        }
        else
        {
            return null;
        }
    }
}

[UsedImplicitly]
public class LogFilePathWithEditTime(string filePath, DateTime editTime)
{
    public string FilePath { get; set; } = filePath;
    public DateTime EditTime { get; set; } = editTime;

    public override string ToString()
    {
        return $"File: {FilePath}, Last Edited: {EditTime}";
    }
}
