using Microsoft.Extensions.DependencyInjection;

namespace NexusMods.Abstractions.Media;

public static class ServiceExtensions
{
    public static IServiceCollection AddMedia(this IServiceCollection serviceCollection)
    {
        return serviceCollection.AddStoredImageModel();
    }
}
