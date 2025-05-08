using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;

namespace NexusMods.Abstractions.Loadouts.Extensions;

/// <summary>
/// Extension methods for working with <see cref="Loadout"/>.
/// </summary>
[PublicAPI]
public static class LoadoutExtensions
{
    public static IEnumerable<LoadoutItemGroup.ReadOnly> GetEnabledGroups(this Loadout.ReadOnly loadout)
    {
        return loadout.Items.OfTypeLoadoutItemGroup().Where(group => group.AsLoadoutItem().IsEnabled());
    }
}
