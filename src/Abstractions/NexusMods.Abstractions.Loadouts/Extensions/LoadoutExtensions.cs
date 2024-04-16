using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.Abstractions.Loadouts.Mods;

namespace NexusMods.Abstractions.Loadouts.Extensions;

/// <summary>
/// Extension methods for working with <see cref="Loadout"/>.
/// </summary>
[PublicAPI]
public static class LoadoutExtensions
{
    private static IEnumerable<Mod> GetEnabledMods(this Loadout loadout, bool onlyEnabledMods)
    {
        return onlyEnabledMods
            ? loadout.Mods.Values.Where(mod => mod.Enabled)
            : loadout.Mods.Values;
    }

    /// <summary>
    /// Gets all enabled mods in the Loadout.
    /// </summary>
    public static IEnumerable<Mod> GetEnabledMods(this Loadout loadout)
    {
        return loadout.GetEnabledMods(onlyEnabledMods: true);
    }

    /// <summary>
    /// Counts all mods that have metadata of type <typeparamref name="T"/>.
    /// </summary>
    public static int CountModsWithMetadata<T>(
        this Loadout loadout,
        bool onlyEnabledMods = true) where T : AModMetadata
    {
        return loadout
            .GetEnabledMods(onlyEnabledMods)
            .Count(mod => mod.HasMetadata<T>());
    }

    /// <summary>
    /// Gets all mods that have metadata of type <typeparamref name="T"/>.
    /// </summary>
    public static IEnumerable<ValueTuple<Mod, T>> GetModsWithMetadata<T>(
        this Loadout loadout,
        bool onlyEnabledMods = true) where T : AModMetadata
    {
        return loadout
            .GetEnabledMods(onlyEnabledMods)
            .Where(mod => mod.HasMetadata<T>())
            .Select(mod => (mod, mod.GetMetadata<T>().Value));
    }

    /// <summary>
    /// Gets the first mod that has metadata of type <typeparamref name="T"/>.
    /// </summary>
    public static Optional<ValueTuple<Mod, T>> GetFirstModWithMetadata<T>(
        this Loadout loadout,
        bool onlyEnabledMods = true) where T : AModMetadata
    {
        return loadout.GetModsWithMetadata<T>(onlyEnabledMods).FirstOrDefault();
    }
}
