using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace NexusMods.Abstractions.NexusModsLibrary;

/// <summary>
/// Extension methods.
/// </summary>
[PublicAPI]
public static class Services
{
    /// <summary>
    /// Extension method.
    /// </summary>
    public static IServiceCollection AddNexusModsLibraryModels(this IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddNexusModsFileMetadataModel()
            .AddNexusModsModPageMetadataModel()
            .AddNexusModsLibraryFileModel()
            .AddNexusModsCollectionMetadataModel()
            .AddNexusModsCollectionRevisionModel()
            .AddNexusModsCollectionLibraryFileModel();
    }
}
