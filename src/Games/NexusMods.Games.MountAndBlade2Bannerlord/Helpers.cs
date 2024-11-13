using System.Runtime.CompilerServices;
using Bannerlord.ModuleManager;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Resources;
using NexusMods.Games.MountAndBlade2Bannerlord.Models;
namespace NexusMods.Games.MountAndBlade2Bannerlord;

public class Helpers
{
    public static async IAsyncEnumerable<ValueTuple<ModLoadoutItem.ReadOnly, ModuleInfoExtended>> GetAllManifestsAsync(
        ILogger logger,
        Loadout.ReadOnly loadout,
        IResourceLoader<ModLoadoutItem.ReadOnly, ModuleInfoExtended> pipeline,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var asyncEnumerable = loadout.Items
            .OfTypeLoadoutItemGroup()
            .OfTypeModLoadoutItem()
            .ToAsyncEnumerable()
            .ConfigureAwait(continueOnCapturedContext: false)
            .WithCancellation(cancellationToken);

        await using var enumerator = asyncEnumerable.GetAsyncEnumerator();
        while (await enumerator.MoveNextAsync())
        {
            var bannerlordMod = enumerator.Current;
            Resource<ModuleInfoExtended> resource;

            try
            {
                resource = await pipeline.LoadResourceAsync(bannerlordMod, cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Exception while getting manifest for `{Name}`", bannerlordMod.AsLoadoutItemGroup().AsLoadoutItem().Name);
                continue;
            }

            yield return (bannerlordMod, resource.Data);
        }
    }
}
