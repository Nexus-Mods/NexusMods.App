using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.Abstractions.Loadouts.Mods;
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

    private static IEnumerable<Mod.ReadOnly> GetEnabledMods(this Loadout.ReadOnly loadout, bool onlyEnabledMods)
    {
        var enumerable = onlyEnabledMods
            ? loadout.Mods.Where(mod => mod.Enabled)
            : loadout.Mods;

        return enumerable.Where(mod => mod.Category == ModCategory.Mod);
    }

    /// <summary>
    /// Gets all enabled mods in the Loadout.
    /// </summary>
    public static IEnumerable<Mod.ReadOnly> GetEnabledMods(this Loadout.ReadOnly loadout)
    {
        return loadout.GetEnabledMods(onlyEnabledMods: true);
    }

    /// <summary>
    /// Counts all mods that have the given metadata.
    /// </summary>
    public static int CountModsWithMetadata(
        this Loadout.ReadOnly loadout,
        IAttribute attribute,
        bool onlyEnabledMods = true)
    {
        return loadout
            .GetEnabledMods(onlyEnabledMods)
            .Count(mod => mod.Contains(attribute));
    }

    /// <summary>
    /// Gets all mods that have metadata of type <typeparamref name="T"/>.
    /// </summary>
    public static IEnumerable<ValueTuple<Mod.ReadOnly, TOuter>> GetModsWithMetadata<TOuter, TInner>(
        this Loadout.ReadOnly loadout,
        ScalarAttribute<TOuter, TInner> attribute,
        bool onlyEnabledMods = true) where TOuter : notnull
    {
        return loadout
            .GetEnabledMods(onlyEnabledMods)
            .Where(mod => mod.Contains(attribute))
            .Select(mod => (mod, attribute.Get(mod)));
    }

    /// <summary>
    /// Gets the first mod that has metadata of type <typeparamref name="T"/>.
    /// </summary>
    public static Optional<ValueTuple<Mod.ReadOnly, TOuter>> GetFirstModWithMetadata<TOuter, TInner>(
        this Loadout.ReadOnly loadout,
        ScalarAttribute<TOuter, TInner> attribute,
        bool onlyEnabledMods = true) where TOuter : notnull
    {
        return loadout.GetModsWithMetadata(attribute, onlyEnabledMods)
            .FirstOrOptional(_ => true);
    }
}
