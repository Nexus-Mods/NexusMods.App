using System.Runtime.CompilerServices;
using DynamicData.Kernel;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Diagnostics.Values;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Abstractions.Resources;
using NexusMods.Abstractions.Telemetry;
using NexusMods.Extensions.BCL;
using NexusMods.Games.StardewValley.Models;
using NexusMods.MnemonicDB.Abstractions;
using StardewModdingAPI;
using StardewModdingAPI.Toolkit;
using StardewModdingAPI.Toolkit.Serialization.Models;

namespace NexusMods.Games.StardewValley.Emitters;

internal static class Helpers
{
    public static readonly NamedLink NexusModsLink = new("Nexus Mods", NexusModsUrlBuilder.GetGameUri(StardewValley.DomainStatic, campaign: NexusModsUrlBuilder.CampaignDiagnostics));
    public static readonly NamedLink SMAPILink = new("Nexus Mods", NexusModsUrlBuilder.GetModUri(StardewValley.DomainStatic, ModId.From(2400), campaign: NexusModsUrlBuilder.CampaignDiagnostics));

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

    public static async ValueTask<IReadOnlyList<ValueTuple<SMAPIManifestLoadoutFile.ReadOnly, Manifest>>> GetAllManifestsAsync(
        ILogger logger,
        IDb db,
        LoadoutId loadoutId,
        bool onlyEnabled,
        IResourceLoader<SMAPIManifestLoadoutFile.ReadOnly, Manifest> pipeline,
        CancellationToken cancellationToken = default)
    {
        var result = new List<ValueTuple<SMAPIManifestLoadoutFile.ReadOnly, Manifest>>();
        var manifestLoadoutItems = SMAPIManifestLoadoutFile.GetAllInLoadout(db, loadoutId, onlyEnabled: onlyEnabled);
        foreach (var manifestLoadoutItem in manifestLoadoutItems)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var manifest = await pipeline.LoadResourceAsync(manifestLoadoutItem, cancellationToken);
                result.Add((manifestLoadoutItem, manifest.Data));
            }
            catch (Exception e)
            {
                logger.LogError(e, "Exception while loading manifest for `{GroupName}`", manifestLoadoutItem.AsLoadoutFile().AsLoadoutItemWithTargetPath().AsLoadoutItem().Parent.AsLoadoutItem().Name);
            }
        }

        return result;
    }
}
