using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.MnemonicDB.Attributes;

namespace NexusMods.Abstractions.Library;

/// <summary>
/// Extension methods.
/// </summary>
[PublicAPI]
public static class Services
{
    /// <summary>
    /// Extension method.
    /// </summary>
    public static IServiceCollection AddLibraryAttributes(this IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddLibraryItemModel()
            .AddLibraryFileModel()
            .AddLocalFileModel()
            .AddLibraryArchiveModel()
            .AddLibraryArchiveFileEntryModel();
    }
}
