using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Diagnostics.Values;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Mods;
using StardewModdingAPI.Toolkit.Serialization.Models;

namespace NexusMods.Games.StardewValley.Emitters;

internal static class Helpers
{
    public static readonly NamedLink NexusModsLink = new("Nexus Mods", new Uri("https://nexusmods.com/stardewvalley"));
    public static readonly NamedLink SMAPILink = new("Nexus Mods", new Uri("https://nexusmods.com/stardewvalley/mods/2400"));

    public static async IAsyncEnumerable<ValueTuple<Mod.Model, Manifest>> GetAllManifestsAsync(
        ILogger logger,
        IFileStore fileStore,
        Loadout.Model loadout,
        bool onlyEnabledMods,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var asyncEnumerable = loadout.Mods
            .Where(mod => !onlyEnabledMods || mod.Enabled)
            .ToAsyncEnumerable()
            .ConfigureAwait(continueOnCapturedContext: false)
            .WithCancellation(cancellationToken);

        await using var enumerator = asyncEnumerable.GetAsyncEnumerator();
        while (await enumerator.MoveNextAsync())
        {
            var mod = enumerator.Current;
            var manifest = await GetManifest(logger, fileStore, mod, cancellationToken);

            if (manifest is null) continue;
            yield return (mod, manifest);
        }
    }

    private static async ValueTask<Manifest?> GetManifest(
        ILogger logger,
        IFileStore fileStore,
        Mod.Model mod,
        CancellationToken cancellationToken)
    {
        try
        {
            return await Interop.GetManifest(fileStore, mod, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            // ignored
            return null;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Exception trying to get manifest for mod {Mod}", mod.Name);
            return null;
        }
    }
}
