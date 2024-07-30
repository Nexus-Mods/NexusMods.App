using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Diagnostics.Values;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Telemetry;
using NexusMods.Extensions.BCL;
using NexusMods.Games.StardewValley.Models;
using StardewModdingAPI.Toolkit.Serialization.Models;

namespace NexusMods.Games.StardewValley.Emitters;

internal static class Helpers
{
    public static readonly NamedLink NexusModsLink = new("Nexus Mods", NexusModsUrlBuilder.CreateGenericUri("https://nexusmods.com/stardewvalley"));
    public static readonly NamedLink SMAPILink = new("Nexus Mods", NexusModsUrlBuilder.CreateDiagnosticUri(StardewValley.GameDomain.Value, "2400"));

    public static bool TryGetSMAPI(Loadout.ReadOnly loadout, out SMAPILoadoutItem.ReadOnly smapi)
    {
        var foundSMAPI = loadout.Items
            .OfTypeLoadoutItemGroup()
            .OfTypeSMAPILoadoutItem()
            .TryGetFirst(x => !x.AsLoadoutItemGroup().AsLoadoutItem().IsIsDisabledMarker, out smapi);

        return foundSMAPI;
    }

    public static async IAsyncEnumerable<ValueTuple<SMAPIModLoadoutItem.ReadOnly, Manifest>> GetAllManifestsAsync(
        ILogger logger,
        IFileStore fileStore,
        Loadout.ReadOnly loadout,
        bool onlyEnabledMods,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var asyncEnumerable = loadout.Items
            .OfTypeLoadoutItemGroup()
            .OfTypeSMAPIModLoadoutItem()
            .Where(x => !onlyEnabledMods || !x.AsLoadoutItemGroup().AsLoadoutItem().IsIsDisabledMarker)
            .ToAsyncEnumerable()
            .ConfigureAwait(continueOnCapturedContext: false)
            .WithCancellation(cancellationToken);

        await using var enumerator = asyncEnumerable.GetAsyncEnumerator();
        while (await enumerator.MoveNextAsync())
        {
            var smapiMod = enumerator.Current;
            var manifest = await GetManifest(logger, fileStore, smapiMod, cancellationToken);

            if (manifest is null) continue;
            yield return (smapiMod, manifest);
        }
    }

    private static async ValueTask<Manifest?> GetManifest(
        ILogger logger,
        IFileStore fileStore,
        SMAPIModLoadoutItem.ReadOnly smapiMod,
        CancellationToken cancellationToken)
    {
        try
        {
            return await Interop.GetManifest(fileStore, smapiMod, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            // ignored
            return null;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Exception trying to get manifest for mod {Mod}", smapiMod.AsLoadoutItemGroup().AsLoadoutItem().Name);
            return null;
        }
    }
}
