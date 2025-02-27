using System.Runtime.CompilerServices;
using DynamicData.Kernel;
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

        // NOTE(erri120): `Major.Minor.Patch` is the only thing the SMAPI API supports
        // in regard to SemanticVersion. Passing a `System.Version` into the constructor
        // will create a `SemanticVersion` with only `Major`, `Minor`, and `Patch` fields.
        // The string parser of `SemanticVersion` accepts more if `allowNonStandard` is enabled,
        // however the SMAPI API will not return any data if a "non-standard" version is passed
        // to it for some reason.
        // See https://github.com/Nexus-Mods/NexusMods.App/pull/2713 for details.
        var localVersion = game
            .GetLocalVersion(loadout.Installation)
            .Convert(static version => new SemanticVersion(version));

        if (localVersion.HasValue) return localVersion.Value;

        // NOTE(erri120): should only be hit during tests
        var vanityVersion = loadout.GameVersion;
        var rawVersion = vanityVersion.Value;

#if DEBUG
        // NOTE(erri120): dumb hack for tests
        var index = rawVersion.IndexOf(".stubbed", StringComparison.OrdinalIgnoreCase);
        if (index != -1)
        {
            rawVersion = rawVersion.AsSpan()[..index].ToString();
        }
#endif

        var gameVersion = new SemanticVersion(rawVersion, allowNonStandard: true);
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
}
