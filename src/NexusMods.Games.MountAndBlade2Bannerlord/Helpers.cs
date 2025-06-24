using System.Runtime.CompilerServices;
using Bannerlord.ModuleManager;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.MountAndBlade2Bannerlord.Models;
using NexusMods.Sdk.Resources;

namespace NexusMods.Games.MountAndBlade2Bannerlord;

public class Helpers
{
    public static async IAsyncEnumerable<ValueTuple<BannerlordModuleLoadoutItem.ReadOnly, ModuleInfoExtended>> GetAllManifestsAsync(
        ILogger logger,
        Loadout.ReadOnly loadout,
        IResourceLoader<BannerlordModuleLoadoutItem.ReadOnly, ModuleInfoExtended> pipeline,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var enumerable = loadout.Items
            .OfTypeLoadoutItemGroup()
            .OfTypeBannerlordModuleLoadoutItem();

        foreach (var bannerlordMod in enumerable)
        {
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
