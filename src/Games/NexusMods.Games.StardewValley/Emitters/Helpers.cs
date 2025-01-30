using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Diagnostics.Values;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.Abstractions.Resources;
using NexusMods.Abstractions.Telemetry;
using NexusMods.Extensions.BCL;
using NexusMods.Games.StardewValley.Models;
using StardewModdingAPI.Toolkit.Serialization.Models;

namespace NexusMods.Games.StardewValley.Emitters;

internal static class Helpers
{
    public static readonly NamedLink NexusModsLink = new("Nexus Mods", NexusModsUrlBuilder.CreateGenericUri("https://nexusmods.com/stardewvalley"));
    public static readonly NamedLink SMAPILink = new("Nexus Mods", NexusModsUrlBuilder.CreateDiagnosticUri(StardewValley.DomainStatic.Value, "2400"));

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
}
