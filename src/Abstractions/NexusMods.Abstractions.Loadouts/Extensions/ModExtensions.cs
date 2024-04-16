using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.Abstractions.Loadouts.Mods;

namespace NexusMods.Abstractions.Loadouts.Extensions;

/// <summary>
/// Extension methods for <see cref="Mod"/>.
/// </summary>
[PublicAPI]
public static class ModExtensions
{
    /// <summary>
    /// Returns true if the mod has metadata of type <typeparamref name="T"/>.
    /// </summary>
    public static bool HasMetadata<T>(this Mod mod) where T : AModMetadata
    {
        return mod.GetMetadata<T>().HasValue;
    }

    /// <summary>
    /// Returns an optional metadata object on the mod, if it exists.
    /// </summary>
    public static Optional<T> GetMetadata<T>(this Mod mod) where T : AModMetadata
    {
        return mod.Metadata.OfType<T>().FirstOrDefault();
    }
}
