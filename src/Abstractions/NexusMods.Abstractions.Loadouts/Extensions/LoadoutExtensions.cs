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
    /// <summary>
    /// Gets all enabled mods in the Loadout.
    /// </summary>
    public static IEnumerable<Mod> GetEnabledMods(this Loadout loadout)
    {
        return loadout.Mods.Values.Where(mod => mod.Enabled);
    }

    /// <summary>
    /// Gets all mods that have metadata of type <typeparamref name="T"/>.
    /// </summary>
    public static IEnumerable<ValueTuple<Mod, T>> GetModsWithMetadata<T>(this Loadout loadout) where T : AModMetadata
    {
        return loadout
            .GetEnabledMods()
            .Where(mod => mod.HasMetadata<T>())
            .Select(mod => (mod, mod.GetMetadata<T>().Value));
    }

    /// <summary>
    /// Gets the first mod that has metadata of type <typeparamref name="T"/>.
    /// </summary>
    public static Optional<ValueTuple<Mod, T>> GetFirstModWithMetadata<T>(
        this Loadout loadout) where T : AModMetadata
    {
        return loadout.GetModsWithMetadata<T>().FirstOrDefault();
    }
}
