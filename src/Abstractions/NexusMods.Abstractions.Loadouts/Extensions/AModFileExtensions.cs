using JetBrains.Annotations;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Serialization;

namespace NexusMods.Abstractions.Loadouts.Extensions;

/// <summary>
/// Extension methods for <see cref="AModFile"/>
/// </summary>
[PublicAPI]
public static class AModFileExtensions
{
    /// <summary>
    /// Checks whether any items in <see cref="AModFile.Metadata"/> are of type
    /// <typeparamref name="T"/>.
    /// </summary>
    public static bool HasMetadata<T>(this AModFile file) where T : IMetadata
    {
        return file.Metadata.Any(m => m is T);
    }
}
