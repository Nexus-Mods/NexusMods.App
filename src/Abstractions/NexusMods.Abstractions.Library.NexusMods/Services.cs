using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.MnemonicDB.Attributes;

namespace NexusMods.Abstractions.Library.NexusModsLibrary;

/// <summary>
/// Extension methods.
/// </summary>
[PublicAPI]
public static class Services
{
    /// <summary>
    /// Extension method.
    /// </summary>
    public static IServiceCollection AddNexusModsLibraryAttributes(this IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddModelDefinition<NexusModsLibraryFile>()
            .AddModelDefinition<NexusModsFileMetadata>()
            .AddModelDefinition<NexusModsModPageMetadata>();
    }
}
