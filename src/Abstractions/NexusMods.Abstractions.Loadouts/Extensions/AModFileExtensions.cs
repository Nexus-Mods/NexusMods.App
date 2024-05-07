using JetBrains.Annotations;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Serialization;
using NexusMods.MnemonicDB.Abstractions;
using File = NexusMods.Abstractions.Loadouts.Files.File;

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
    public static bool HasMetadata(this File.Model file, IAttribute attribute)
    {
        return file.Contains(attribute);
    }
}
