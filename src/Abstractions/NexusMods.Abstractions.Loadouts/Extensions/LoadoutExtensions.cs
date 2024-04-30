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
    private static IEnumerable<Mod.Model> GetEnabledMods(this Loadout.Model loadout, bool onlyEnabledMods)
    {
        return onlyEnabledMods
            ? loadout.Mods.Where(mod => mod.Enabled)
            : loadout.Mods;
    }

    /// <summary>
    /// Gets all enabled mods in the Loadout.
    /// </summary>
    public static IEnumerable<Mod.Model> GetEnabledMods(this Loadout.Model loadout)
    {
        return loadout.GetEnabledMods(onlyEnabledMods: true);
    }

    /// <summary>
    /// Counts all mods that have the given metadata.
    /// </summary>
    public static int CountModsWithMetadata(
        this Loadout.Model loadout,
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
    public static IEnumerable<ValueTuple<Mod.Model, TOuter>> GetModsWithMetadata<TOuter, TInner>(
        this Loadout.Model loadout,
        ScalarAttribute<TOuter, TInner> attribute,
        bool onlyEnabledMods = true)
    {
        return loadout
            .GetEnabledMods(onlyEnabledMods)
            .Where(mod => mod.Contains(attribute))
            .Select(mod => (mod, mod.Get(attribute)));
    }

    /// <summary>
    /// Gets the first mod that has metadata of type <typeparamref name="T"/>.
    /// </summary>
    public static Optional<ValueTuple<Mod.Model, TOuter>> GetFirstModWithMetadata<TOuter, TInner>(
        this Loadout.Model loadout,
        ScalarAttribute<TOuter, TInner> attribute,
        bool onlyEnabledMods = true)
    {
        return loadout.GetModsWithMetadata(attribute, onlyEnabledMods)
            .FirstOrOptional(_ => true);
    }
}
