using Microsoft.Extensions.DependencyInjection;

namespace NexusMods.Abstractions.Media;

/// <summary>
/// Extension methods.
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Adds media.
    /// </summary>
    public static IServiceCollection AddMedia(this IServiceCollection serviceCollection)
    {
        return serviceCollection.AddStoredImageModel();
    }
}
